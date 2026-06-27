using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class UsersAdmin : ComponentBase
    {
        [Inject] private UserManagementService UserMgmt { get; set; } = null!;
        [Inject] private AuthenticationStateProvider AuthState { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager Nav { get; set; } = null!;

        // ── Estado general ───────────────────────────────────────────────
        private bool loading = true;
        private bool auditLoading = false;

        // ── Datos ────────────────────────────────────────────────────────
        private List<UserRoleSummary> roleSummaries = new();
        private List<UserDetailDto> allUsers = new();
        private List<AuditLogDto> globalFeed = new();
        private List<AuditLogDto> selectedUserLogs = new();

        // ── Usuario seleccionado para el panel de logs ───────────────────
        private UserDetailDto? selectedUser;

        // ── Filtros ──────────────────────────────────────────────────────
        private UserFilterModel filter = new();
        private string searchText = string.Empty;
        private string? roleFilter;
        private string? statusFilter;
        private string sortBy = "LastActivity";

        // ── Tab activa ───────────────────────────────────────────────────
        private int activeTab = 0;

        // ── Usuario actual (quien está logueado) ─────────────────────────
        private string currentUserId = string.Empty;

        // ── Totales rápidos ──────────────────────────────────────────────
        private int TotalUsers => roleSummaries.Sum(r => r.Total);
        private int TotalActive => roleSummaries.Sum(r => r.Active);
        private int TotalInactive => roleSummaries.Sum(r => r.Inactive);

        protected override async Task OnInitializedAsync()
        {
            // Obtener el usuario logueado
            var authState = await AuthState.GetAuthenticationStateAsync();
            currentUserId = authState.User.FindFirst("sub")?.Value
                         ?? authState.User.FindFirst("UserId")?.Value
                         ?? string.Empty;

            // ── Protección interna — [Authorize] no funciona en recarga con LocalStorage
            var authStateCheck = await AuthState.GetAuthenticationStateAsync();
            if (!authStateCheck.User.Identity?.IsAuthenticated ?? true)
            {
                Nav.NavigateTo("/login", replace: true);
                return;
            }
            if (!authStateCheck.User.IsInRole("Admin"))
            {
                Nav.NavigateTo("/", replace: true);
                return;
            }

            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            loading = true;
            StateHasChanged();

            try
            {
                roleSummaries = await UserMgmt.GetRoleSummariesAsync();
                await ApplyFiltersAsync();
                globalFeed = await UserMgmt.GetGlobalAuditFeedAsync(100);
            }
            finally
            {
                loading = false;
                StateHasChanged();
            }
        }

        private async Task ApplyFiltersAsync()
        {
            filter = new UserFilterModel
            {
                SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,
                SortBy = sortBy
            };

            allUsers = await UserMgmt.GetAllUsersAsync(filter);
        }

        private async Task OnSearchChanged(string value)
        {
            searchText = value;
            await ApplyFiltersAsync();
        }

        private async Task OnRoleFilterChanged(string? value)
        {
            roleFilter = value;
            await ApplyFiltersAsync();
        }

        private async Task OnStatusFilterChanged(string? value)
        {
            statusFilter = value;
            await ApplyFiltersAsync();
        }

        private async Task OnSortChanged(string value)
        {
            sortBy = value;
            await ApplyFiltersAsync();
        }

        // ── Seleccionar usuario y cargar su historial ────────────────────
        private async Task SelectUserAsync(UserDetailDto user)
        {
            if (selectedUser?.UserId == user.UserId)
            {
                selectedUser = null;
                selectedUserLogs = new();
                return;
            }

            selectedUser = user;
            auditLoading = true;
            StateHasChanged();

            selectedUserLogs = await UserMgmt.GetUserAuditLogsAsync(user.UserId, 50);
            auditLoading = false;
            StateHasChanged();
        }

        // ── Activar / Desactivar cuenta ──────────────────────────────────
        private async Task ToggleUserStatusAsync(UserDetailDto user)
        {
            var ok = await UserMgmt.ToggleUserStatusAsync(user.UserId, currentUserId);
            if (ok)
            {
                var msg = user.IsActive ? "desactivada" : "activada";
                Snackbar.Add($"Cuenta de {user.FullName} {msg}.", Severity.Success);
                // ── Protección interna — [Authorize] no funciona en recarga con LocalStorage
                var authStateCheck = await AuthState.GetAuthenticationStateAsync();
                if (!authStateCheck.User.Identity?.IsAuthenticated ?? true)
                {
                    Nav.NavigateTo("/login", replace: true);
                    return;
                }
                if (!authStateCheck.User.IsInRole("Admin"))
                {
                    Nav.NavigateTo("/", replace: true);
                    return;
                }

                await LoadDataAsync();

                // Refrescar si ese usuario estaba seleccionado
                if (selectedUser?.UserId == user.UserId)
                {
                    selectedUser = allUsers.FirstOrDefault(u => u.UserId == user.UserId);
                    selectedUserLogs = await UserMgmt.GetUserAuditLogsAsync(user.UserId, 50);
                }
            }
            else
            {
                Snackbar.Add("Error al cambiar el estado de la cuenta.", Severity.Error);
            }
        }

        // ── Helpers visuales ─────────────────────────────────────────────
        private static string RoleChipClass(string role) => role switch
        {
            "Admin" => "dl-chip-admin",
            "Empleado" => "dl-chip-empleado",
            "Cliente" => "dl-chip-cliente",
            _ => "dl-chip-default"
        };

        private static string RoleIcon(string role) => role switch
        {
            "Admin" => Icons.Material.Filled.AdminPanelSettings,
            "Empleado" => Icons.Material.Filled.Badge,
            "Cliente" => Icons.Material.Filled.Person,
            _ => Icons.Material.Filled.AccountCircle
        };

        private static string ActionIcon(string? action) => action switch
        {
            var a when a != null && a.Contains("Creó") => Icons.Material.Filled.AddCircle,
            var a when a != null && a.Contains("Editó") => Icons.Material.Filled.Edit,
            var a when a != null && a.Contains("Eliminó") => Icons.Material.Filled.Delete,
            var a when a != null && a.Contains("Activada") => Icons.Material.Filled.CheckCircle,
            var a when a != null && a.Contains("Desactivada") => Icons.Material.Filled.Cancel,
            var a when a != null && a.Contains("Login") => Icons.Material.Filled.Login,
            var a when a != null && a.Contains("Stock") => Icons.Material.Filled.Inventory,
            _ => Icons.Material.Filled.FiberManualRecord
        };

        private static string FormatRelativeTime(DateTime? dt)
        {
            if (dt == null) return "Nunca";
            var diff = DateTime.Now - dt.Value;
            if (diff.TotalMinutes < 1) return "Hace un momento";
            if (diff.TotalMinutes < 60) return $"Hace {(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"Hace {(int)diff.TotalHours} h";
            if (diff.TotalDays < 7) return $"Hace {(int)diff.TotalDays} días";
            return dt.Value.ToString("dd MMM yyyy, hh:mm tt");
        }
    }
}
