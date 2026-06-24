using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class ProductDetail
    {
        [Parameter] public int ProductId { get; set; }
        [Inject] public ICatalogService CatalogService { get; set; } = default!;

        ProductDetailDto? Product;
        List<ComboDto> Combos = new();
        List<PromotionDto> Promotions = new();
        bool _loading = true;

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

        private Color GetPromoBadgeColor(PromotionDto promo)
        {
            if (promo.IsPayXGetY) return Color.Success;
            if (promo.HasDiscount) return Color.Error;
            if (promo.HasSpecialPrice) return Color.Warning;
            return Color.Info;
        }

        // ── Agregar producto al carrito ──
        private async Task AddToCart(int productId)
        {
            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) { Nav.NavigateTo("/login"); return; }

                await CartService.AddProductToCartAsync(userId, productId, 1, 1);
                Snackbar.Add("Producto agregado al carrito ✓", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }

        // ── Agregar combo al carrito ──
        private async Task AddComboToCart(int comboId)
        {
            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) { Nav.NavigateTo("/login"); return; }

                await CartService.AddComboToCartAsync(userId, comboId, 1, 1);
                Snackbar.Add("Combo agregado al carrito ✓", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }

        // ── Agregar promoción al carrito ──
        private async Task AddPromotionToCart(PromotionDto promo)
        {
            try
            {
                var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) { Nav.NavigateTo("/login"); return; }

                var qty = promo.IsPayXGetY ? (promo.RequiredQuantity ?? 1) : 1;
                await CartService.AddPromotionToCartAsync(userId, promo.PromotionId, qty, 1);
                Snackbar.Add($"Promoción «{promo.Title}» agregada al carrito ✓", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error: {ex.Message}", Severity.Error);
            }
        }
    }
}
