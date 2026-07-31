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

        private const int StoreId = 1;
        private HashSet<int> _expandedPromoIds = new();

        protected override async Task OnInitializedAsync()
        {
            await Load();
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
                    Snackbar.Add($"Ya tienes la promoción «{promo.Title}» en tu carrito", Severity.Info);
                    return;
                }

                var qty = promo.IsPayXGetY ? (promo.RequiredQuantity ?? 1) : 1;
                await CartService.AddPromotionToCartAsync(userId, promo.PromotionId, qty, StoreId);
                Snackbar.Add($"Promoción «{promo.Title}» agregada al carrito", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }
    }
}
