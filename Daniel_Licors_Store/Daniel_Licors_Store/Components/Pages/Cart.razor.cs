using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Cart;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages;

public partial class Cart
{
    [Inject] private ICartService CartService { get; set; } = default!;
    [Inject] private IWhatsAppService WhatsAppService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ILogger<Cart> Logger { get; set; } = default!;

    private CartViewDto? _cartView;
    private List<CartItemDisplayDto> _filteredProducts = new();
    private List<CartComboDisplayDto> _filteredCombos = new();
    private List<CartPromotionDisplayDto> _filteredPromotions = new();
    private string _searchTerm = "";
    private string _filterCategory = "";
    private bool _loading = true;
    private string _currentUserId = "";
    private int _storeId = 1;

    private int _selectedCount = 0;
    private decimal _selectedPrice = 0;

    private void RecalcSelected()
    {
        if (_cartView == null) { _selectedCount = 0; _selectedPrice = 0; return; }
        _selectedCount = _cartView.Products.Where(p => p.IsSelected).Sum(p => p.Quantity)
                       + _cartView.Combos.Where(c => c.IsSelected).Sum(c => c.Quantity)
                       + _cartView.Promotions.Where(pr => pr.IsSelected).Sum(pr => pr.Quantity);
        _selectedPrice = _cartView.Products.Where(p => p.IsSelected).Sum(p => p.SubTotal)
                       + _cartView.Combos.Where(c => c.IsSelected).Sum(c => c.SubTotal)
                       + _cartView.Promotions.Where(pr => pr.IsSelected).Sum(pr => pr.SubTotal);
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                Nav.NavigateTo("/login");
                return;
            }

            _currentUserId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst("userId")?.Value
                ?? string.Empty;

            if (string.IsNullOrEmpty(_currentUserId))
            {
                Logger.LogWarning("Usuario autenticado pero sin ID en los claims");
                Nav.NavigateTo("/login");
                return;
            }

            await LoadCart();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error en OnInitializedAsync");
            Snackbar.Add($"Error al cargar: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadCart()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
            {
                Snackbar.Add("No se pudo obtener tu sesión. Inicia sesión de nuevo.", Severity.Error);
                await Task.Delay(800);
                Nav.NavigateTo("/login");
                return;
            }

            _cartView = await CartService.GetCartAsync(_currentUserId, _storeId);
            _cartView ??= new CartViewDto();
            RecalcSelected();
            FilterItems();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al cargar carrito para usuario {UserId}", _currentUserId);
            Snackbar.Add($"Error al cargar carrito: {ex.Message}", Severity.Error);
        }
    }

    private void FilterItems()
    {
        var search = _searchTerm?.Trim() ?? "";
        var cat = _filterCategory?.Trim().ToLower() ?? "";

        _filteredProducts = _cartView?.Products
            .Where(p => string.IsNullOrEmpty(search) || p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(p => string.IsNullOrEmpty(cat) || cat == "producto")
            .ToList() ?? new();

        if (cat == "producto")
        {
            _filteredCombos = new();
        }
        else
        {
            _filteredCombos = _cartView?.Combos
                .Where(c => string.IsNullOrEmpty(search) || c.ComboName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Where(c =>
                    string.IsNullOrEmpty(cat) ||
                    (cat == "combo" && !c.ComboName.Contains("Promo", StringComparison.OrdinalIgnoreCase)) ||
                    (cat == "promo" && c.ComboName.Contains("Promo", StringComparison.OrdinalIgnoreCase))
                )
                .ToList() ?? new();

            if (cat == "promo")
                _filteredProducts = new();
        }

        // Filtrar promociones reales de CartViewDto.Promotions
        _filteredPromotions = _cartView?.Promotions
            .Where(pr => string.IsNullOrEmpty(search) || pr.PromotionName.Contains(search, StringComparison.OrdinalIgnoreCase))
            .Where(pr => string.IsNullOrEmpty(cat) || cat == "promo")
            .ToList() ?? new();

        if (cat == "combo")
            _filteredPromotions = new();

        StateHasChanged();
    }

    private void ClearFilters()
    {
        _searchTerm = "";
        _filterCategory = "";
        FilterItems();
    }

    private async Task RemoveItem(int itemId, string itemType)
    {
        if (string.IsNullOrWhiteSpace(_currentUserId))
        {
            Snackbar.Add("Sesión expirada.", Severity.Warning);
            Nav.NavigateTo("/login");
            return;
        }

        try
        {
            _cartView = await CartService.RemoveItemAsync(_currentUserId, itemId, itemType);
            RecalcSelected();
            FilterItems();
            Snackbar.Add("Artículo eliminado", Severity.Info);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al eliminar item {ItemId}", itemId);
            Snackbar.Add($"Error al eliminar: {ex.Message}", Severity.Error);
        }
    }

    private async Task UpdateQuantity(int itemId, int newQuantity, string itemType)
    {
        if (string.IsNullOrWhiteSpace(_currentUserId))
        {
            Snackbar.Add("Sesión expirada.", Severity.Warning);
            Nav.NavigateTo("/login");
            return;
        }

        try
        {
            if (newQuantity <= 0)
            {
                Snackbar.Add("La cantidad debe ser mayor a 0", Severity.Warning);
                return;
            }

            _cartView = await CartService.UpdateItemQuantityAsync(_currentUserId, itemId, newQuantity, itemType);
            RecalcSelected();
            FilterItems();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al actualizar cantidad item {ItemId}", itemId);
            Snackbar.Add($"Error al actualizar cantidad: {ex.Message}", Severity.Error);

            // El contador (+) del item ya se había incrementado visualmente
            // antes de llamar al servicio; si el servidor lo rechazó (p.ej.
            // por stock insuficiente), recargamos el carrito para que la
            // cantidad mostrada vuelva a coincidir con la real.
            await LoadCart();
        }
    }

    private void UpdateSelection(int itemId, bool isSelected, string itemType)
    {
        try
        {
            if (itemType == "product")
            {
                var item = _cartView?.Products.FirstOrDefault(p => p.CartItemId == itemId);
                if (item != null) item.IsSelected = isSelected;
            }
            else if (itemType == "combo")
            {
                var item = _cartView?.Combos.FirstOrDefault(c => c.CartComboId == itemId);
                if (item != null) item.IsSelected = isSelected;
            }
            else if (itemType == "promotion")
            {
                var item = _cartView?.Promotions.FirstOrDefault(pr => pr.CartPromotionId == itemId);
                if (item != null) item.IsSelected = isSelected;
            }

            RecalcSelected();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al actualizar selección de item {ItemId}", itemId);
        }
    }

    private void SelectAll()
    {
        _cartView?.Products.ForEach(p => p.IsSelected = true);
        _cartView?.Combos.ForEach(c => c.IsSelected = true);
        _cartView?.Promotions.ForEach(pr => pr.IsSelected = true);
        RecalcSelected();
        StateHasChanged();
    }

    private void DeselectAll()
    {
        _cartView?.Products.ForEach(p => p.IsSelected = false);
        _cartView?.Combos.ForEach(c => c.IsSelected = false);
        _cartView?.Promotions.ForEach(pr => pr.IsSelected = false);
        RecalcSelected();
        StateHasChanged();
    }

    private async Task ClearCart()
    {
        if (string.IsNullOrWhiteSpace(_currentUserId)) { Nav.NavigateTo("/login"); return; }

        try
        {
            bool? confirm = await DialogService.ShowMessageBox(
                "¿Vaciar Carrito?",
                "¿Estás seguro de que deseas eliminar todos los productos del carrito?",
                yesText: "Sí, vaciar", cancelText: "Cancelar");

            if (confirm == true)
            {
                await CartService.ClearCartAsync(_currentUserId);
                _cartView = await CartService.GetCartAsync(_currentUserId, _storeId);
                RecalcSelected();
                FilterItems();
                Snackbar.Add("Carrito vaciado.", Severity.Info);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error al vaciar carrito");
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task CheckoutAll()
    {
        SelectAll();
        await PerformCheckout(false);
    }

    private async Task CheckoutSelected()
    {
        if (_selectedCount == 0)
        {
            Snackbar.Add("Selecciona al menos un artículo primero.", Severity.Warning);
            return;
        }
        await PerformCheckout(true);
    }

    private async Task PerformCheckout(bool selectedOnly)
    {
        if (string.IsNullOrWhiteSpace(_currentUserId))
        {
            Snackbar.Add("Sesión expirada.", Severity.Warning);
            Nav.NavigateTo("/login");
            return;
        }

        try
        {
            // El servicio ahora devuelve siempre el recibo completo (todos los items)
            var receipt = await CartService.GenerateReceiptAsync(_currentUserId, false);

            if (receipt == null || !receipt.Items.Any())
            {
                Snackbar.Add("No hay artículos para comprar.", Severity.Warning);
                return;
            }

            // FIX: si es selectedOnly, filtramos aquí en el frontend usando IsSelected del _cartView
            if (selectedOnly && _cartView != null)
            {
                var selectedProductIds = _cartView.Products
                    .Where(p => p.IsSelected)
                    .Select(p => p.CartItemId)
                    .ToHashSet();

                var selectedComboIds = _cartView.Combos
                    .Where(c => c.IsSelected)
                    .Select(c => c.CartComboId)
                    .ToHashSet();

                receipt.Items = receipt.Items
                    .Where(i => (i.ItemType == "product" && selectedProductIds.Contains(i.ItemId)) ||
                                (i.ItemType == "combo" && selectedComboIds.Contains(i.ItemId)))
                    .ToList();

                receipt.TotalAmount = receipt.Items.Sum(i => i.SubTotal);
            }

            if (!receipt.Items.Any())
            {
                Snackbar.Add("No hay artículos seleccionados para comprar.", Severity.Warning);
                return;
            }

            var defaultPhone = "+1234567890";
            var whatsappLink = WhatsAppService.GenerateWhatsAppLink(defaultPhone,
                WhatsAppService.GenerateReceiptMessage(receipt));

            var parameters = new DialogParameters
            {
                { "Receipt", receipt },
                { "WhatsAppLink", whatsappLink }
            };

            var dialogResult = await DialogService.ShowAsync<ReceiptDialog>("Tu Recibo", parameters);
            var result = await dialogResult.Result;

            if (!result.Canceled && result.Data != null)
            {
                await CartService.CheckoutAsync(_currentUserId, new List<int>(), "");
                Snackbar.Add("¡Compra completada! Revisa WhatsApp para el recibo.", Severity.Success);
            }
            else
            {
                Snackbar.Add("Pedido cancelado. Tu carrito sigue igual.", Severity.Info);
            }

            _cartView = await CartService.GetCartAsync(_currentUserId, _storeId);
            RecalcSelected();
            FilterItems();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error en PerformCheckout para usuario {UserId}", _currentUserId);
            Snackbar.Add($"Error en la compra: {ex.Message}", Severity.Error);
        }
    }
}
