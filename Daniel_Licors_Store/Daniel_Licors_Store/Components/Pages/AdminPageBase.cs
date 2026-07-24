using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Daniel_Licors_Store.Components.Pages
{
    /// <summary>
    /// Clase base para TODAS las páginas que requieren estar autenticado
    /// y tener un rol específico (Admin, Empleado, etc.).
    ///
    /// Por qué existe: antes, cada página admin repetía a mano el mismo
    /// bloque de "si no está logueado -> /login, si no tiene el rol -> /".
    /// Si una página nueva se te olvidaba pegarlo, quedaba completamente
    /// abierta para cualquiera que supiera la URL — sin ningún aviso, ni
    /// en tiempo de compilación ni en tiempo de ejecución.
    ///
    /// Con esto, el chequeo vive en UN solo lugar y OnInitializedAsync
    /// está sellado (sealed) — ninguna página derivada puede sobreescribirlo
    /// y "saltarse" sin querer la validación. Las páginas ponen su propia
    /// lógica de carga de datos en OnInitializedAuthorizedAsync, que solo
    /// se ejecuta si el usuario ya pasó el chequeo.
    ///
    /// Uso:
    ///   public partial class Combos : AdminPageBase
    ///   {
    ///       protected override string[] AllowedRoles => new[] { "Admin", "Empleado" };
    ///
    ///       protected override async Task OnInitializedAuthorizedAsync()
    ///       {
    ///           // lo que antes tenías en OnInitializedAsync, sin el chequeo
    ///       }
    ///   }
    /// </summary>
    public abstract class AdminPageBase : ComponentBase
    {
        [Inject] protected AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
        [Inject] protected NavigationManager Nav { get; set; } = default!;

        /// <summary>
        /// Roles permitidos en esta página. Por defecto solo "Admin";
        /// las páginas que también permiten "Empleado" la sobreescriben.
        /// </summary>
        protected virtual string[] AllowedRoles => new[] { "Admin" };

        /// <summary>El usuario autenticado actual (una vez pasó el chequeo).</summary>
        protected ClaimsPrincipal CurrentUser { get; private set; } = new ClaimsPrincipal(new ClaimsIdentity());

        /// <summary>UserId del usuario logueado (null si aún no se resolvió).</summary>
        protected string? CurrentUserId { get; private set; }

        /// <summary>True una vez que el usuario pasó el chequeo de auth/rol.</summary>
        protected bool IsAuthorized { get; private set; }

        // sealed: ninguna página derivada puede sobreescribir esto y
        // saltarse el chequeo sin querer.
        protected sealed override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            CurrentUser = authState.User;

            // Protección interna — [Authorize] no funciona en recarga con LocalStorage auth
            if (!(CurrentUser.Identity?.IsAuthenticated ?? false))
            {
                Nav.NavigateTo("/login", replace: true);
                return;
            }

            if (!AllowedRoles.Any(role => CurrentUser.IsInRole(role)))
            {
                Nav.NavigateTo("/", replace: true);
                return;
            }

            CurrentUserId = CurrentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? CurrentUser.FindFirst("sub")?.Value
                          ?? CurrentUser.FindFirst("UserId")?.Value;

            IsAuthorized = true;
            await OnInitializedAuthorizedAsync();
        }

        /// <summary>
        /// Las páginas derivadas ponen aquí lo que antes tenían en
        /// OnInitializedAsync (cargar datos, etc.) — se ejecuta SOLO
        /// si el usuario ya pasó el chequeo de arriba.
        /// </summary>
        protected virtual Task OnInitializedAuthorizedAsync() => Task.CompletedTask;

        /// <summary>
        /// Re-chequeo defensivo para usar justo antes de una acción sensible
        /// (ej. activar/desactivar una cuenta), no solo al cargar la página —
        /// por si la sesión expiró o el rol cambió mientras la página ya
        /// estaba abierta. Devuelve false y navega fuera si ya no es válido.
        /// </summary>
        protected async Task<bool> EnsureStillAuthorizedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!(user.Identity?.IsAuthenticated ?? false))
            {
                Nav.NavigateTo("/login", replace: true);
                return false;
            }

            if (!AllowedRoles.Any(role => user.IsInRole(role)))
            {
                Nav.NavigateTo("/", replace: true);
                return false;
            }

            return true;
        }
    }
}
