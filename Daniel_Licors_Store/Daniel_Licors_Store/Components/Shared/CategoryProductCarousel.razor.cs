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

        // Referencia al elemento del DOM
        private ElementReference swiperElement;
        private bool _swiperInicializado = false;
        private int _previousProductsCount = 0;

        protected void VerProducto(int productId) => Nav.NavigateTo($"/product/{productId}");
        protected void AgregarAlCarrito(int productId) { /* futuro carrito */ }

        // Corremos esto cada vez que Blazor actualiza parámetros (cuando llegan los productos de la BD)
        protected override async Task OnParametersSetAsync()
        {
            // Si la cantidad de productos cambia (ej: de 0 a N cuando carga de la BD), 
            // reseteamos la bandera para forzar a Swiper a reconstruirse.
            if (Products != null && Products.Count != _previousProductsCount)
            {
                _swiperInicializado = false;
                _previousProductsCount = Products.Count;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Solo inicializamos si hay productos reales en la lista y no lo hemos inicializado ya
            if (Products != null && Products.Any() && !_swiperInicializado)
            {
                try
                {
                    // Le pasamos la referencia exacta de ESTE carrusel al JS
                    await JS.InvokeVoidAsync("initProductSwiper", swiperElement);
                    await JS.InvokeVoidAsync("initScrollReveal");
                    _swiperInicializado = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inicializando Swiper: {ex.Message}");
                }
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
                { "ContentText", "¿Estás seguro de que deseas eliminar este producto? Esta acción no se puede deshacer." }
            };

            var dialog = DialogService.Show<ConfirmDialog>("Confirmar eliminación", parameters);
            var result = await dialog.Result;

            if (!result.Canceled)
            {
                await ProductService.DeleteProductAsync(productId);
                ProductState.NotifyChange();
            }
        }
    }
}