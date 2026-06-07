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

        // Helper: color del badge según tipo de promoción
        private Color GetPromoBadgeColor(PromotionDto promo)
        {
            if (promo.IsPayXGetY) return Color.Success;
            if (promo.HasDiscount) return Color.Error;
            if (promo.HasSpecialPrice) return Color.Warning;
            return Color.Info;
        }
    }
}
