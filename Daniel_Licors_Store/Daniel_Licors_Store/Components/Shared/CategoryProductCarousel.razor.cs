using Daniel_Licors_Store.Components.Product;
using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Shared
{
    public partial class CategoryProductCarousel : IAsyncDisposable
    {
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public int CategoryId { get; set; }
        [Parameter] public List<ProductResponseModel> Products { get; set; } = new();
        [Parameter] public bool IsLoading { get; set; } = true;

        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IProductService ProductService { get; set; } = default!;
        [Inject] private ProductStateService ProductState { get; set; } = default!;
        [Inject] private ICartService CartService { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;

        private bool _disposed = false;

        // ── Carrusel: índice activo ─────────────────────────────────────────
        private int _activeIndex = 0;

        // ── Touch / drag ─────────────────────────────────────────────────────
        private double _touchStartX = 0;
        private double _mouseStartX = 0;
        private bool _isDragging = false;
        private const double SwipeThreshold = 50;

        // Touch
        private void OnTouchStart(TouchEventArgs e)
        {
            if (e.Touches.Length > 0)
                _touchStartX = e.Touches[0].ClientX;
        }

        private void OnTouchEnd(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length == 0) return;
            double delta = e.ChangedTouches[0].ClientX - _touchStartX;
            if (Math.Abs(delta) >= SwipeThreshold)
            {
                if (delta < 0) Next();
                else Prev();
            }
        }

        // Mouse drag
        private void OnMouseDown(MouseEventArgs e)
        {
            _mouseStartX = e.ClientX;
            _isDragging = true;
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            double delta = e.ClientX - _mouseStartX;
            if (Math.Abs(delta) >= SwipeThreshold)
            {
                if (delta < 0) Next();
                else Prev();
            }
        }

        private void OnMouseLeave(MouseEventArgs e)
        {
            _isDragging = false;
        }

        // ── Carousel logic ───────────────────────────────────────────────────
        private int GetPosition(int index)
        {
            if (Products == null || Products.Count == 0) return 99;
            int count = Products.Count;
            int raw = index - _activeIndex;
            while (raw > count / 2) raw -= count;
            while (raw < -count / 2) raw += count;
            return raw;
        }

        private string GetCardClass(int pos) => pos switch
        {
            0 => "dlc-pos-center",
            1 => "dlc-pos-right1",
            -1 => "dlc-pos-left1",
            2 => "dlc-pos-right2",
            -2 => "dlc-pos-left2",
            _ => "dlc-pos-hidden"
        };

        private void Next()
        {
            if (Products == null || Products.Count == 0) return;
            _activeIndex = (_activeIndex + 1) % Products.Count;
        }

        private void Prev()
        {
            if (Products == null || Products.Count == 0) return;
            _activeIndex = (_activeIndex - 1 + Products.Count) % Products.Count;
        }

        private void GoTo(int index) => _activeIndex = index;

        private void CardClicked(int i)
        {
            if (i != _activeIndex) _activeIndex = i;
        }

        // ── Fábrica de íconos por categoría ──────────────────────────────────
        // Le da a cada categoría un ícono coherente (cerveza, botella, copa de
        // vino, etc.) en vez de un emoji fijo para todas.
        private static string GetCategoryIcon(string? categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return Icons.Material.Filled.LocalBar;

            var name = categoryName.Trim().ToLowerInvariant();

            if (name.Contains("cerveza")) return Icons.Material.Filled.SportsBar;
            if (name.Contains("ron")) return Icons.Material.Filled.Liquor;
            if (name.Contains("vodka")) return Icons.Material.Filled.Liquor;
            if (name.Contains("whisky") || name.Contains("whiskey")) return Icons.Material.Filled.LocalBar;
            if (name.Contains("tequila")) return Icons.Material.Filled.LocalBar;
            if (name.Contains("vino") || name.Contains("champagne") || name.Contains("champán")) return Icons.Material.Filled.WineBar;
            if (name.Contains("energiz")) return Icons.Material.Filled.Bolt;
            if (name.Contains("mixer")) return Icons.Material.Filled.LocalDrink;
            if (name.Contains("snack")) return Icons.Material.Filled.Fastfood;
            if (name.Contains("dulce")) return Icons.Material.Filled.Cake;

            return Icons.Material.Filled.LocalBar;
        }

        // ── Acciones de producto ─────────────────────────────────────────────
        protected async Task AgregarAlCarrito(int productId)
        {
            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity == null || !user.Identity.IsAuthenticated)
                {
                    Snackbar.Add("Debes iniciar sesión para añadir productos al carrito", Severity.Warning);
                    Nav.NavigateTo("/login");
                    return;
                }

                var userId =
                    user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("sub")?.Value
                    ?? string.Empty;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    Snackbar.Add("No se pudo identificar tu usuario", Severity.Error);
                    return;
                }

                await CartService.AddProductToCartAsync(userId, productId, 1, storeId: 1);
                Snackbar.Add("🛒 Producto añadido al carrito", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }

        void EditarProducto(int productId)
        {
            var parameters = new DialogParameters { { "ProductId", productId } };
            DialogService.Show<ProductDialog>("Editar Bebida", parameters);
        }

        async Task DeleteProduct(int productId)
        {
            var parameters = new DialogParameters
            {
                { "ContentText", "¿Deseas ocultar este producto? Ya no aparecerá en el catálogo, carrito, combos ni promociones, pero no se borra: puedes volver a mostrarlo desde el Panel de Control → Elementos Ocultos." },
                { "ButtonText", "Ocultar" }
            };
            var dialog = DialogService.Show<ConfirmDialog>("Confirmar ocultamiento", parameters);
            var result = await dialog.Result;
            if (!result.Canceled)
            {
                try
                {
                    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                    var userId = authState.User.FindFirst("UserId")?.Value
                              ?? authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                    await ProductService.DeleteProductAsync(productId, userId);
                    ProductState.NotifyChange();
                    Snackbar.Add("Producto ocultado. Puedes reactivarlo desde el Panel de Control.", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error: {ex.Message}", Severity.Error);
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
