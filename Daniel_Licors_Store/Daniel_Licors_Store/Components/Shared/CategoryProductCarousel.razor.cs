using Daniel_Licors_Store.Components.Product;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using Daniel_Licors_Store.Components.Shared;

namespace Daniel_Licors_Store.Components.Shared
{
    public partial class CategoryProductCarousel : IAsyncDisposable
    {
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public int CategoryId { get; set; }
        [Parameter] public List<ProductResponseModel> Products { get; set; } = new();

        // FIX: nuevo parámetro que recibe el estado de carga del padre
        // para mostrar skeleton y evitar que el carousel aparezca con lista vacía
        [Parameter] public bool IsLoading { get; set; } = true;

        [Inject] private NavigationManager Nav { get; set; } = default!;
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private IProductService ProductService { get; set; } = default!;
        [Inject] private ProductStateService ProductState { get; set; } = default!;

        private ElementReference swiperElement;
        private bool _swiperInicializado = false;
        private int _previousProductsCount = -1;
        private bool _disposed = false;

        protected void VerProducto(int productId) => Nav.NavigateTo($"/producto/{productId}");
        protected void AgregarAlCarrito(int productId) { /* futuro carrito */ }

        protected override async Task OnParametersSetAsync()
        {
            // Resetea flag solo si la cantidad real de productos cambió
            if (Products != null && Products.Count != _previousProductsCount)
            {
                _swiperInicializado = false;
                _previousProductsCount = Products.Count;
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (_disposed) return;

            // FIX: Solo intentamos inicializar Swiper si:
            // 1. Hay productos reales
            // 2. No está en estado de carga
            // 3. No lo inicializamos ya
            if (!IsLoading && Products != null && Products.Any() && !_swiperInicializado)
            {
                try
                {
                    // FIX: pequeño delay para dejar que Chrome termine
                    // su ciclo de layout antes de que Swiper manipule el DOM.
                    // Edge/Brave son más tolerantes; Chrome requiere este tick.
                    await Task.Delay(50);

                    if (_disposed) return;

                    await JS.InvokeVoidAsync("initProductSwiper", swiperElement);
                    await JS.InvokeVoidAsync("initScrollReveal");
                    _swiperInicializado = true;
                }
                catch (JSDisconnectedException)
                {
                    // Circuit cerrado — normal cuando el usuario navega
                }
                catch (ObjectDisposedException)
                {
                    // Componente ya destruido — ignorar
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Carousel] Error Swiper: {ex.Message}");
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

        // FIX: IAsyncDisposable para destruir Swiper desde JS
        // antes de que Blazor destruya el nodo del DOM.
        // Esto previene el insertBefore sobre nodo nulo en navegaciones.
        public async ValueTask DisposeAsync()
        {
            _disposed = true;
            try
            {
                if (_swiperInicializado)
                {
                    await JS.InvokeVoidAsync("destroyProductSwiper", swiperElement);
                }
            }
            catch
            {
                // Si el circuit ya cerró, no hay nada que hacer
            }
        }
    }
}
