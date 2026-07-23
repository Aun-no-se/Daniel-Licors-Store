using Daniel_Licors_Store.Models.Users.Auth;
using Daniel_Licors_Store.Services.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.RegularExpressions;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Register
    {
        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;

        private RegisterModel model = new();
        private string? errorMessage;
        private bool isLoading;
        private bool success;
        private bool _showPass;
        private bool _showConfirm;

        // Misma regex que usa AuthService — validar aquí evita un viaje al
        // servidor solo para enterarse de que el correo está mal escrito.
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // ── Password handlers (sin @bind-Value para evitar conflicto) ──────────
        private void OnPasswordChanged(string v) { model.Password = v; StateHasChanged(); }
        private void OnConfirmChanged(string v) { model.ConfirmPassword = v; StateHasChanged(); }
        private void ToggleShowPass() => _showPass = !_showPass;
        private void ToggleShowConfirm() => _showConfirm = !_showConfirm;

        // ── Fortaleza de contraseña ─────────────────────────────────────────────
        private int StrengthScore
        {
            get
            {
                if (string.IsNullOrEmpty(model.Password)) return 0;
                int s = 0;
                if (model.Password.Length >= 8) s++;
                if (model.Password.Length >= 12) s++;
                if (model.Password.Any(char.IsUpper)) s++;
                if (model.Password.Any(char.IsDigit)) s++;
                if (model.Password.Any(ch => !char.IsLetterOrDigit(ch))) s++;
                return s;
            }
        }

        private string StrengthColor => StrengthScore switch
        {
            <= 1 => "#F44336",
            2 => "#FF9800",
            3 => "#FFC107",
            4 => "#8BC34A",
            _ => "#4CAF50"
        };

        private string StrengthLabel => StrengthScore switch
        {
            <= 1 => "Muy débil",
            2 => "Débil",
            3 => "Aceptable",
            4 => "Fuerte",
            _ => "Muy fuerte ✓"
        };

        // ── Submit ──────────────────────────────────────────────────────────────
        private async Task HandleRegister()
        {
            errorMessage = null;

            // ── Validación cliente — mismas reglas que aplica AuthService,
            // así el usuario ve el problema al instante, sin ida y vuelta
            // al servidor y sin depender solo del mensaje de la excepción.
            if (string.IsNullOrWhiteSpace(model.Name) || string.IsNullOrWhiteSpace(model.LastName))
            { errorMessage = "Nombre y apellido son requeridos."; return; }

            if (model.Age < 18 || model.Age > 120)
            { errorMessage = "Debes tener entre 18 y 120 años para registrarte."; return; }

            var email = model.Email?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            { errorMessage = "El correo electrónico no es válido."; return; }

            if (string.IsNullOrWhiteSpace(model.Password) || model.Password.Length < 8)
            { errorMessage = "La contraseña debe tener al menos 8 caracteres."; return; }

            if (!model.Password.Any(char.IsUpper))
            { errorMessage = "La contraseña debe tener al menos una letra mayúscula."; return; }

            if (!model.Password.Any(char.IsDigit))
            { errorMessage = "La contraseña debe contener al menos un número."; return; }

            if (model.Password != model.ConfirmPassword)
            { errorMessage = "Las contraseñas no coinciden."; return; }

            isLoading = true;

            try
            {
                await AuthService.RegisterAsync(model);
                success = true;
                StateHasChanged();
                await Task.Delay(1500);
                Nav.NavigateTo("/login");
            }
            catch (Exception ex)
            {
                // Última línea de defensa por si el backend cambia sus reglas
                // y el frontend queda desactualizado.
                errorMessage = ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
