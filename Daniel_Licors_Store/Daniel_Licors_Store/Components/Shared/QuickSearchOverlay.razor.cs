using System.Security.Claims;
using Blazored.LocalStorage;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Shared;

public partial class QuickSearchOverlay : IAsyncDisposable
{
    [Inject] private QuickSearchState SearchState { get; set; } = default!;
    [Inject] private IProductSearchService SearchService { get; set; } = default!;
    [Inject] private ICartService CartService { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private const int MaxRecents = 8;

    private DotNetObjectReference<QuickSearchOverlay>? _dotRef;
    private CancellationTokenSource? _cts;
    private bool _wasOpen;
    private bool _scrollActive;

    private string _query = string.Empty;
    private bool _hasText;
    private int _active = -1;

    private string _userId = string.Empty;
    private string _storageKey = "dls_recent_searches_guest";
    private bool _loaded;

    private List<string> _recents = new();
    private List<string> _categories = new();
    private List<SearchProductDto> _results = new();
    private List<SearchProductDto> _purchased = new();
    private List<SearchProductDto> _popular = new();
    private readonly HashSet<int> _favorites = new();
    private readonly HashSet<int> _adding = new();
    private readonly HashSet<int> _added = new();

    // Con 2 letras ya se busca; con menos se muestra la memoria y los atajos.
    private bool IsSearching => _query.Trim().Length >= 2;
    private bool CanAdd => !string.IsNullOrEmpty(_userId);

    protected override void OnInitialized()
    {
        SearchState.OnChange += OnStateChanged;
        Nav.LocationChanged += OnLocationChanged;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _dotRef = DotNetObjectReference.Create(this);
                await JS.InvokeVoidAsync("quickSearch.registerHotkeys", _dotRef);
            }
            catch { /* sin JS todavía: el botón de la barra sigue funcionando */ }
        }

        if (_scrollActive)
        {
            _scrollActive = false;
            try { await JS.InvokeVoidAsync("quickSearch.scrollActive"); } catch { }
        }
    }

    // ── Abrir / cerrar ───────────────────────────────────────────────────

    private void OnStateChanged() => _ = InvokeAsync(HandleStateAsync);

    private void OnLocationChanged(object? sender, Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e)
    {
        // Botón "atrás" del navegador o cualquier navegación: el panel no se queda abierto encima.
        if (SearchState.IsOpen) SearchState.Close();
    }

    private async Task HandleStateAsync()
    {
        try
        {
            if (SearchState.IsOpen && !_wasOpen)
            {
                _wasOpen = true;
                ResetState();
                StateHasChanged();

                await JS.InvokeVoidAsync("quickSearch.lock", true);
                await JS.InvokeVoidAsync("quickSearch.focus", "qs-input");
                await LoadHomeAsync();
            }
            else if (!SearchState.IsOpen && _wasOpen)
            {
                _wasOpen = false;
                _cts?.Cancel();
                StateHasChanged();
                await JS.InvokeVoidAsync("quickSearch.lock", false);
            }
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    private void ResetState()
    {
        _query = string.Empty;
        _hasText = false;
        _active = -1;
        _results = new List<SearchProductDto>();
        _added.Clear();
        _adding.Clear();
        _loaded = false;
    }

    [JSInvokable]
    public Task ToggleFromHotkey()
    {
        SearchState.Toggle();
        return Task.CompletedTask;
    }

    private void CloseSearch() => SearchState.Close();

    // ── Carga de lo que se muestra al abrir ──────────────────────────────

    private async Task LoadHomeAsync()
    {
        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = auth.User;

            _userId = user.Identity?.IsAuthenticated == true
                ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value ?? string.Empty
                : string.Empty;

            // Una memoria por cliente: si dos personas comparten el teléfono, no se mezclan.
            _storageKey = $"dls_recent_searches_{(string.IsNullOrEmpty(_userId) ? "guest" : _userId)}";

            var recentsTask = ReadRecentsAsync();
            var purchasedTask = string.IsNullOrEmpty(_userId)
                ? Task.FromResult(new List<SearchProductDto>())
                : SearchService.GetPurchasedAsync(_userId, 12);
            var popularTask = SearchService.GetPopularAsync(4);
            var categoriesTask = SearchService.GetTopCategoriesAsync(8);

            try { await Task.WhenAll(recentsTask, purchasedTask, popularTask, categoriesTask); }
            catch { /* cada tarea se revisa abajo; una que falle no tumba a las demás */ }

            if (recentsTask.IsCompletedSuccessfully) _recents = recentsTask.Result;
            if (purchasedTask.IsCompletedSuccessfully)
            {
                var all = purchasedTask.Result;
                _favorites.Clear();
                foreach (var p in all) _favorites.Add(p.ProductId);
                _purchased = all.Take(4).ToList();
            }
            if (popularTask.IsCompletedSuccessfully) _popular = popularTask.Result;
            if (categoriesTask.IsCompletedSuccessfully) _categories = categoriesTask.Result;
        }
        catch
        {
            // Sin datos de memoria igual se puede buscar.
        }
        finally
        {
            _loaded = true;
        }

        // Si el cliente ya empezó a escribir mientras cargaba, se refresca con favoritos incluidos.
        if (IsSearching) await RunSearchAsync();

        await InvokeAsync(StateHasChanged);
    }

    // ── Escribir y buscar ────────────────────────────────────────────────

    private async Task OnInput(ChangeEventArgs e)
    {
        _query = e.Value?.ToString() ?? string.Empty;
        _hasText = _query.Length > 0;
        _active = -1;

        _cts?.Cancel();
        var cts = _cts = new CancellationTokenSource();

        // Pequeña espera para no buscar en cada letra cuando el cliente escribe rápido.
        try { await Task.Delay(110, cts.Token); }
        catch (TaskCanceledException) { return; }

        await RunSearchAsync();
    }

    private async Task RunSearchAsync()
    {
        if (!IsSearching)
        {
            _results = new List<SearchProductDto>();
            return;
        }

        try
        {
            _results = await SearchService.SearchAsync(_query.Trim(), _favorites, 8);
        }
        catch
        {
            _results = new List<SearchProductDto>();
        }

        // La primera ya queda marcada: Enter abre la mejor coincidencia.
        _active = _results.Count > 0 ? 0 : -1;
    }

    private async Task UseTerm(string term)
    {
        _query = term;
        _hasText = true;
        await JS.InvokeVoidAsync("quickSearch.setValue", "qs-input", term);
        await RunSearchAsync();
    }

    private async Task ClearQuery()
    {
        _query = string.Empty;
        _hasText = false;
        _active = -1;
        _results = new List<SearchProductDto>();
        await JS.InvokeVoidAsync("quickSearch.setValue", "qs-input", string.Empty);
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Escape":
                SearchState.Close();
                break;

            case "ArrowDown":
                if (IsSearching && _results.Count > 0)
                {
                    _active = (_active + 1) % _results.Count;
                    _scrollActive = true;
                }
                break;

            case "ArrowUp":
                if (IsSearching && _results.Count > 0)
                {
                    _active = (_active - 1 + _results.Count) % _results.Count;
                    _scrollActive = true;
                }
                break;

            case "Enter":
                // Si pulsó Enter antes de que terminara la pequeña espera, se busca ya mismo.
                _cts?.Cancel();
                await RunSearchAsync();
                if (_results.Count > 0)
                    await OpenProductAsync(_results[Math.Max(_active, 0)]);
                break;
        }
    }

    // ── Acciones sobre una bebida ────────────────────────────────────────

    private async Task OpenProductAsync(SearchProductDto product)
    {
        if (IsSearching) await SaveRecentAsync(_query);

        SearchState.Close();
        Nav.NavigateTo($"/producto/{product.ProductId}");
    }

    private async Task AddAsync(SearchProductDto product)
    {
        if (string.IsNullOrEmpty(_userId) || !_adding.Add(product.ProductId))
            return;

        try
        {
            await CartService.AddProductToCartAsync(_userId, product.ProductId, 1, storeId: 1);

            _added.Add(product.ProductId);
            Snackbar.Add($"🛒 {ProductNaming.Compose(product.Name, product.Size)} añadido al carrito", Severity.Success);

            // El ✓ se queda un momento y vuelve el "+" por si quiere otra unidad.
            _ = ResetAddedLaterAsync(product.ProductId);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _adding.Remove(product.ProductId);
        }
    }

    private async Task ResetAddedLaterAsync(int productId)
    {
        try
        {
            await Task.Delay(1400);
            await InvokeAsync(() =>
            {
                _added.Remove(productId);
                StateHasChanged();
            });
        }
        catch { /* el panel ya se cerró */ }
    }

    // ── Memoria de búsquedas (en el teléfono/PC del cliente) ─────────────

    private async Task<List<string>> ReadRecentsAsync()
    {
        try
        {
            return await LocalStorage.GetItemAsync<List<string>>(_storageKey) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task SaveRecentAsync(string? term)
    {
        term = term?.Trim();
        if (string.IsNullOrEmpty(term) || term.Length < 2) return;

        var key = ProductSearchService.Normalize(term);
        _recents.RemoveAll(r => ProductSearchService.Normalize(r) == key);
        _recents.Insert(0, term);
        if (_recents.Count > MaxRecents)
            _recents.RemoveRange(MaxRecents, _recents.Count - MaxRecents);

        await PersistRecentsAsync();
    }

    private async Task RemoveRecent(string term)
    {
        _recents.Remove(term);
        await PersistRecentsAsync();
    }

    private async Task ClearRecents()
    {
        _recents.Clear();
        try { await LocalStorage.RemoveItemAsync(_storageKey); } catch { }
    }

    private async Task PersistRecentsAsync()
    {
        try { await LocalStorage.SetItemAsync(_storageKey, _recents); }
        catch { /* sin almacenamiento: simplemente no se recuerda */ }
    }

    // ── Limpieza ─────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        SearchState.OnChange -= OnStateChanged;
        Nav.LocationChanged -= OnLocationChanged;
        _cts?.Cancel();

        try { await JS.InvokeVoidAsync("quickSearch.lock", false); }
        catch { }

        _dotRef?.Dispose();
    }
}
