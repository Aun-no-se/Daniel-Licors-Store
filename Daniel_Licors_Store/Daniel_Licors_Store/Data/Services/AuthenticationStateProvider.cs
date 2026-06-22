using Blazored.LocalStorage;
using Daniel_Licors_Store.Models.Users.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Daniel_Licors_Store.Data.Services
{
    /// <summary>
    /// CustomAuthStateProvider: guarda la sesión en LocalStorage
    /// y expone el estado de autenticación a Blazor.
    /// Se mantiene LocalStorage porque es Blazor Server con pre-rendering desactivado.
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private UserSession? _cachedSession;

        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Usa caché en memoria primero para evitar llamadas dobles a LocalStorage
                _cachedSession ??= await _localStorage.GetItemAsync<UserSession>("userSession");

                if (_cachedSession == null)
                    return Anonymous();

                // Verificar que la sesión no haya expirado (30 min de inactividad)
                if (DateTime.Now - _cachedSession.LastActivity > TimeSpan.FromMinutes(30))
                {
                    await Logout();
                    return Anonymous();
                }

                return BuildState(_cachedSession);
            }
            catch
            {
                // Si LocalStorage falla (pre-rendering, etc.), retornar anónimo sin explotar
                return Anonymous();
            }
        }

        public async Task MarkUserAsAuthenticated(UserSession session)
        {
            if (string.IsNullOrWhiteSpace(session.UserId))
                throw new InvalidOperationException("UserId no puede ser vacío al autenticar.");

            session.LastActivity = DateTime.Now;
            _cachedSession = session;

            await _localStorage.SetItemAsync("userSession", session);
            NotifyAuthenticationStateChanged(Task.FromResult(BuildState(session)));
        }

        public async Task RefreshActivity()
        {
            try
            {
                var session = await _localStorage.GetItemAsync<UserSession>("userSession");
                if (session != null)
                {
                    session.LastActivity = DateTime.Now;
                    _cachedSession = session;
                    await _localStorage.SetItemAsync("userSession", session);
                }
            }
            catch { /* silencioso */ }
        }

        public async Task Logout()
        {
            _cachedSession = null;
            await _localStorage.RemoveItemAsync("userSession");
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        }

        // ── helpers privados ──────────────────────────────────────────────
        private static AuthenticationState Anonymous()
            => new(new ClaimsPrincipal(new ClaimsIdentity()));

        private static AuthenticationState BuildState(UserSession session)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.UserId),
                new(ClaimTypes.Name,           session.Email)
            };
            claims.AddRange(session.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "customAuth");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}
