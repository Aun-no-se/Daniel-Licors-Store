using Daniel_Licors_Store.Models.Users;
using Daniel_Licors_Store.Models.Users.Auth;
using Daniel_Licors_Store.Services.Auth;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class CreateUser : AdminPageBase
    {
        // AllowedRoles no se sobreescribe: por defecto AdminPageBase ya
        // restringe a solo "Admin", que es lo que esta página necesita.

        [Inject] public IAuthService AuthService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;

        private AdminRegisterModel model = new() { RoleId = 2 }; // Empleado por defecto
        private bool isLoading;
        private bool submitted;
        private bool success;
        private string? errorMessage;
        private bool _showPass;
        private bool _showConfirm;

        // ── Validaciones en tiempo real ─────────────────────────────────────────
        private static readonly System.Text.RegularExpressions.Regex EmailRegex =
            new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                System.Text.RegularExpressions.RegexOptions.Compiled |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        private bool EmailValid =>
            !string.IsNullOrWhiteSpace(model.Email) && EmailRegex.IsMatch(model.Email);

        private bool PasswordValid =>
            !string.IsNullOrEmpty(model.Password) &&
            model.Password.Length >= 8 &&
            model.Password.Any(char.IsUpper) &&
            model.Password.Any(char.IsDigit);

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

        private int StrengthPercent => StrengthScore * 20;
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
            _ => "Muy fuerte"
        };

        // ── Rol seleccionado ────────────────────────────────────────────────────
        private string RoleName => model.RoleId switch
        {
            1 => "Admin",
            2 => "Empleado",
            _ => "Cliente"
        };

        // ── Acciones ────────────────────────────────────────────────────────────
        private void SelectRole(int roleId) => model.RoleId = roleId;

        // Métodos nombrados para evitar conflicto de ValueChanged con @bind-Value
        private void OnPasswordChanged(string v) { model.Password = v; StateHasChanged(); }
        private void OnConfirmPasswordChanged(string v) { model.ConfirmPassword = v; StateHasChanged(); }
        private void ToggleShowPass() => _showPass = !_showPass;
        private void ToggleShowConfirm() => _showConfirm = !_showConfirm;

        private async Task HandleCreate()
        {
            submitted = true;
            errorMessage = null;

            // Validación cliente-side antes de ir al servidor
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(model.Name))
                errors.Add("El nombre es requerido.");

            if (string.IsNullOrWhiteSpace(model.LastName))
                errors.Add("El apellido es requerido.");

            if (!EmailValid)
                errors.Add("El correo electrónico no es válido.");

            if (model.Age < 18 || model.Age > 100)
                errors.Add("La edad debe estar entre 18 y 100 años.");

            if (!PasswordValid)
                errors.Add("La contraseña debe tener mínimo 8 caracteres, una mayúscula y un número.");

            if (model.Password != model.ConfirmPassword)
                errors.Add("Las contraseñas no coinciden.");

            if (model.RoleId < 1 || model.RoleId > 3)
                errors.Add("Selecciona un rol válido.");

            if (errors.Any())
            {
                errorMessage = string.Join(" · ", errors);
                return;
            }

            isLoading = true;

            try
            {
                await AuthService.CreateUserByAdminAsync(model);
                success = true;

                Snackbar.Add($"Usuario {model.Name} {model.LastName} creado exitosamente",
                             Severity.Success,
                             cfg => { cfg.VisibleStateDuration = 3000; });
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ResetForm()
        {
            model = new AdminRegisterModel { RoleId = 2 };
            submitted = false;
            success = false;
            errorMessage = null;
            _showPass = false;
            _showConfirm = false;
        }
    }
}
