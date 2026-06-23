using Daniel_Licors_Store.Components.Product;
using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
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

        public ValueTask DisposeAsync()
        {
            _disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
