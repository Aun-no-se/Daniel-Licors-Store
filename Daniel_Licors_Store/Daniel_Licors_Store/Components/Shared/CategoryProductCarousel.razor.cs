using Daniel_Licors_Store.Components.Product;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Daniel_Licors_Store.Components.Shared;


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
        [Inject] private ProductStateService ProductState { get; set; } = default!;


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
                await JS.InvokeVoidAsync("initScrollReveal"); 
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

        async Task DeleteProduct(int productId)
        {
            var parameters = new DialogParameters
    {
        { "ContentText", "¿Estás seguro de que deseas eliminar este producto? Esta acción no se puede deshacer." }
    };

            var dialog = DialogService.Show<ConfirmDialog>(
                "Confirmar eliminación",
                parameters
            );

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await ProductService.DeleteProductAsync(productId);
                ProductState.NotifyChange();
            }
        }
    }
}