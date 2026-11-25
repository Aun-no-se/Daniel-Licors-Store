using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class ProductDetail
    {
        // ── Ruta ──────────────────────────────────────────────────────────────
        [Parameter] public int ProductId { get; set; }

        // ── Inyecciones (todas centralizadas aquí, nada en el .razor) ──────────
        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private ICartService CartService { get; set; } = default!;
        [Inject] private ICatalogService CatalogService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        // ── Estado ───────────────────────────────────────────────────────────
        protected ProductDetailDto? Product;
        protected List<ComboDto> Combos = new();
        protected List<PromotionDto> Promotions = new();
        protected bool _loading = true;
        protected bool _adding = false;
        protected int _quantity = 1;
        protected int _qtyVersion;   // repinta la casilla cuando se corrige lo escrito

        private const int StoreId = 1;
        private HashSet<int> _expandedPromoIds = new();

        private int _loadedProductId;

        // OnParametersSetAsync (y no OnInitializedAsync): al cambiar de tamaño
        // (/producto/5 → /producto/6) Blazor reutiliza la página, y así se recarga
        // con la otra bebida.
        protected override async Task OnParametersSetAsync()
        {
            if (_loadedProductId == ProductId) return;

            _loadedProductId = ProductId;
            _quantity = 1;
            _expandedPromoIds.Clear();
            await Load();
        }

        // ── Cambiar a otro tamaño de la misma bebida ─────────────────────────
        protected void OpenVariant(ProductSizeVariantDto variant)
        {
            if (!variant.IsCurrent)
                Nav.NavigateTo($"/producto/{variant.ProductId}");
        }

        private async Task Load()
        {
            _loading = true;
            try
            {
                Product = await CatalogService.GetProductDetailAsync(ProductId);
                if (Product is not null)
                {
                    Combos = await CatalogService.GetCombosByProductAsync(ProductId);
                    Promotions = await CatalogService.GetPromotionsByProductAsync(ProductId);
                }
            }
            finally
            {
                _loading = false;
            }
        }

        // ── Selector de cantidad ─────────────────────────────────────────────
        protected void IncreaseQuantity()
        {
            if (Product != null && _quantity < Product.Stock)
                _quantity++;
        }

        // Número escrito directamente en la casilla (ej. 25 botellas de una vez)
        protected void OnQuantityTyped(ChangeEventArgs e)
        {
            if (Product == null) return;

            var digits = new string((e.Value?.ToString() ?? string.Empty).Where(char.IsDigit).ToArray());
            var qty = int.TryParse(digits, out var parsed) ? parsed : _quantity;

            if (qty < 1) qty = 1;

            if (Product.Stock > 0 && qty > Product.Stock)
            {
                qty = Product.Stock;
                Snackbar.Add($"Solo hay {Product.Stock} disponible(s).", Severity.Warning);
            }

            _quantity = qty;
            _qtyVersion++;
        }

        protected void DecreaseQuantity()
        {
            if (_quantity > 1)
                _quantity--;
        }

        // ── Barra visual de disponibilidad (decorativa) ─────────────────────
        protected double StockPercentage()
        {
            if (Product is null || Product.Stock <= 0) return 0;
            const int visualCap = 25;
            return Math.Min(100, Product.Stock * 100.0 / visualCap);
        }

        // ── Badges / colores de promoción ───────────────────────────────────
        protected Color GetPromoBadgeColor(PromotionDto promo)
        {
            if (promo.IsPayXGetY) return Color.Success;
            if (promo.HasDiscount) return Color.Error;
            if (promo.HasSpecialPrice) return Color.Warning;
            return Color.Info;
        }

        // ── Expandir/colapsar "Ver detalle" de una promoción ────────────────
        protected bool IsPromoExpanded(int promotionId) => _expandedPromoIds.Contains(promotionId);

        protected void TogglePromoDetail(int promotionId)
        {
            if (!_expandedPromoIds.Remove(promotionId))
            {
                _expandedPromoIds.Add(promotionId);
            }
        }

        // ── Helper: obtiene el UserId autenticado, o manda a /login ─────────
        private async Task<string?> GetUserIdOrRedirectAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = authState.User.FindFirst(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Nav.NavigateTo("/login");
                return null;
            }
            return userId;
        }

        // ── Agregar producto al carrito ──────────────────────────────────────
        protected async Task AddToCart(int productId)
        {
            if (_adding) return;
            _adding = true;
            try
            {
                var userId = await GetUserIdOrRedirectAsync();
                if (userId is null) return;

                await CartService.AddProductToCartAsync(userId, productId, _quantity, StoreId);
                Snackbar.Add("Producto agregado al carrito", Severity.Success);
                _quantity = 1;
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
            finally
            {
                _adding = false;
            }
        }

        // ── Agregar combo al carrito ─────────────────────────────────────────
        protected async Task AddComboToCart(int comboId)
        {
            try
            {
                var userId = await GetUserIdOrRedirectAsync();
                if (userId is null) return;

                var nombreCombo = Combos.FirstOrDefault(c => c.ComboId == comboId)?.Name ?? "el combo";

                // Igual que en ComboDetail: el combo no se bloquea por stock ya
                // reservado con productos sueltos u otros combos, solo por si
                // ese mismo combo ya está en el carrito.
                var carritoActual = await CartService.GetCartAsync(userId, StoreId);
                var yaEstaEnCarrito = carritoActual.Combos.Any(c => c.ComboId == comboId);

                if (yaEstaEnCarrito)
                {
                    Snackbar.Add($"Ya tienes «{nombreCombo}» en tu carrito", Severity.Info);
                    return;
                }

                await CartService.AddComboToCartAsync(userId, comboId, 1, StoreId);
                Snackbar.Add("Combo agregado al carrito", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }

        // ── Agregar promoción al carrito ─────────────────────────────────────
        protected async Task AddPromotionToCart(PromotionDto promo)
        {
            try
            {
                var userId = await GetUserIdOrRedirectAsync();
                if (userId is null) return;

                // A diferencia de un producto (que sí tiene sentido seguir
                // sumando unidades), una promoción es un trato puntual: si ya
                // está en el carrito, avisamos en vez de volver a agregarla.
                var carritoActual = await CartService.GetCartAsync(userId, StoreId);
                var yaEstaEnCarrito = carritoActual.Promotions.Any(p => p.PromotionId == promo.PromotionId);

                if (yaEstaEnCarrito)
                {
                    Snackbar.Add($"Ya tienes la promoción «{promo.Title}» en tu carrito. Si quieres más, súbela con + en el carrito.", Severity.Info);
                    return;
                }

                // Cada unidad de promoción es un paquete completo (en "Lleva X Paga Y": X botellas).
                await CartService.AddPromotionToCartAsync(userId, promo.PromotionId, 1, StoreId);
                Snackbar.Add(promo.IsPayXGetY
                    ? $"Promoción «{promo.Title}» agregada: {promo.RequiredQuantity} botellas, pagas {promo.PayQuantity}"
                    : $"Promoción «{promo.Title}» agregada al carrito", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }
    }
}
