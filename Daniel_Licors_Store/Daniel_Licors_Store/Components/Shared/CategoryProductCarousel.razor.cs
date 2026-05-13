using Daniel_Licors_Store.Components.Product;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;


namespace Daniel_Licors_Store.Components.Shared
{
    public partial class CategoryProductCarousel
    {
        [Parameter] public string Title { get; set; } = string.Empty;

        [Parameter] public int CategoryId { get; set; }

        [Parameter] public List<ProductResponseModel> Products { get; set; } = new();

        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IProductService ProductService { get; set; } = default!;

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

        void EditarProducto(int productId)
        {
            var parameters = new DialogParameters
    {
        { "ProductId", productId }
    };

            DialogService.Show<ProductDialog>("Editar Bebida", parameters);
        }

        void BorrarProducto(int productId)
        {
            // luego lo conectamos al repo
            Console.WriteLine($"Borrar producto {productId}");
        }
    }
}