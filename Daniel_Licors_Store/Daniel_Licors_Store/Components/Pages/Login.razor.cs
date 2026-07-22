using Daniel_Licors_Store.Models.Users.Auth;
using Daniel_Licors_Store.Services.Auth;
using Daniel_Licors_Store.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Login
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private LoginModel model = new();
        private string? error;
        private bool isLoading;

        private InputType _passwordInput = InputType.Password;
        private string _passwordIcon = Icons.Material.Filled.VisibilityOff;
        private bool _showPass;

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
                Nav.NavigateTo("/", replace: true);
        }

        private void TogglePass()
        {
            _showPass = !_showPass;
            _passwordInput = _showPass ? InputType.Text : InputType.Password;
            _passwordIcon = _showPass ? Icons.Material.Filled.Visibility : Icons.Material.Filled.VisibilityOff;
        }

        private async Task HandleLogin()
        {
            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                error = "Por favor ingresa tu correo y contraseña.";
                return;
            }

            error = null;
            isLoading = true;

            try
            {
                var result = await AuthService.ValidateLoginAsync(model);

                // Bloqueada o credenciales inválidas: el mensaje ya viene listo,
                // sin necesidad de atrapar ninguna excepción.
                if (result.User == null)
                {
                    error = result.ErrorMessage ?? "El correo o la contraseña son incorrectos.";
                    return;
                }

                var session = new UserSession
                {
                    UserId = result.User.UserId,
                    Email = result.User.Email ?? string.Empty,
                    FullName = result.User.FullName ?? string.Empty,
                    Roles = result.Roles,
                    LastActivity = DateTime.Now
                };

                var customAuth = (CustomAuthStateProvider)AuthStateProvider;
                await customAuth.MarkUserAsAuthenticated(session);

                Nav.NavigateTo("/", forceLoad: true);
            }
            catch (Exception ex)
            {
                // Esto solo debería dispararse ante un error real e inesperado
                // (ej. la base de datos no responde), no por login/bloqueo.
                error = $"Error inesperado: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
