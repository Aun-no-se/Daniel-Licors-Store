using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;


namespace Daniel_Licors_Store.Components.Shared
{
    public partial class CategoryProductCarousel
    {
        [Parameter] public string Title { get; set; } = string.Empty;

        [Parameter] public int CategoryId { get; set; }

        [Parameter] public List<ProductResponseModel> Products { get; set; } = new();

        [Inject] private NavigationManager Nav { get; set; } = default!;

        protected void VerProducto(int productId)
        {
            Nav.NavigateTo($"/product/{productId}");
        }

        protected void AgregarAlCarrito(int productId)
        {
            // futuro carrito
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JS.InvokeVoidAsync("initProductSwiper");
                await JS.InvokeVoidAsync("initScrollReveal"); // 🔥 FALTABA ESTO
            }
        }
    }
}