using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Combo;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using System.Security.Claims;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class ComboDetail
    {
        [Parameter] public int ComboId { get; set; }

        [Inject] private ICartService CartService { get; set; } = default!;
        [Inject] private ISnackbar Snackbar { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private const int StoreId = 1;

        ComboDetailDto? Combo;
        bool _loading = true;
        bool _adding = false;

        string ComboImage => Combo?.Products?.FirstOrDefault()?.ImageUrl
                     ?? "/images/combo-default.png";

        protected override async Task OnInitializedAsync()
        {
            await Load();
        }

        private async Task Load()
        {
            _loading = true;

            try
            {
                Combo = await ComboService.GetComboDetailAsync(ComboId);
            }
            finally
            {
                _loading = false;
            }
        }

        void GoToProduct(int productId)
        {
            Nav.NavigateTo($"/producto/{productId}");
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

        // ── Agregar combo al carrito ─────────────────────────────────────────
        protected async Task AddComboToCart()
        {
            if (Combo is null || _adding) return;
            _adding = true;
            try
            {
                var userId = await GetUserIdOrRedirectAsync();
                if (userId is null) return;

                // El combo NO se bloquea por stock ya reservado con productos
                // sueltos u otros combos — si el cliente lo quiere comprar
                // empaquetado, se le deja. La única restricción es que no
                // esté ya el mismo combo en el carrito.
                var carritoActual = await CartService.GetCartAsync(userId, StoreId);
                var yaEstaEnCarrito = carritoActual.Combos.Any(c => c.ComboId == Combo.ComboId);

                if (yaEstaEnCarrito)
                {
                    Snackbar.Add($"Ya tienes el combo «{Combo.Name}» en tu carrito", Severity.Info);
                    return;
                }

                await CartService.AddComboToCartAsync(userId, Combo.ComboId, 1, StoreId);
                Snackbar.Add($"Combo «{Combo.Name}» agregado al carrito", Severity.Success);
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
    }
}