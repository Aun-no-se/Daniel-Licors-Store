using Blazored.LocalStorage;
using Daniel_Licors_Store.Models.Users.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Daniel_Licors_Store.Data.Services
{
    /// <summary>
    /// CustomAuthStateProvider: guarda la sesión en LocalStorage
    /// FIRMADA con HMAC-SHA256 (via SessionSigner).
    ///
    /// Flujo de seguridad:
    ///   Login  → SessionSigner.Sign(session)  → guarda token firmado en LS
    ///   Lectura → SessionSigner.Verify(token) → si firma inválida → anónimo
    ///
    /// Cualquier edición manual del LocalStorage invalida la firma y
    /// el usuario queda como anónimo automáticamente.
    /// </summary>
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private const string SessionKey = "userSession_v2"; // clave distinta para invalidar sesiones viejas sin firma

        private readonly ILocalStorageService _localStorage;
        private readonly SessionSigner _signer;
        private UserSession? _cachedSession;

        public CustomAuthStateProvider(ILocalStorageService localStorage, SessionSigner signer)
        {
            _localStorage = localStorage;
            _signer       = signer;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                if (_cachedSession != null)
                    return ValidateAndBuild(_cachedSession) ?? Anonymous();

                // Leer el token firmado del LocalStorage
                var token = await _localStorage.GetItemAsStringAsync(SessionKey);
                if (string.IsNullOrEmpty(token))
                    return Anonymous();

                // ── VERIFICACIÓN DE FIRMA ─────────────────────────────────────
                // Si alguien editó el token en DevTools, Verify devuelve null
                var session = _signer.Verify<UserSession>(token);
                if (session == null)
                {
                    // Token manipulado — limpiar y rechazar
                    await _localStorage.RemoveItemAsync(SessionKey);
                    return Anonymous();
                }

                // Verificar expiración (30 min de inactividad)
                if (DateTime.Now - session.LastActivity > TimeSpan.FromMinutes(30))
                {
                    await Logout();
                    return Anonymous();
                }

                _cachedSession = session;
                return BuildState(session);
            }
            catch
            {
                return Anonymous();
            }
        }

        public async Task MarkUserAsAuthenticated(UserSession session)
        {
            if (string.IsNullOrWhiteSpace(session.UserId))
                throw new InvalidOperationException("UserId no puede ser vacío al autenticar.");

            session.LastActivity = DateTime.Now;
            _cachedSession = session;

            // ── GUARDAR FIRMADO ───────────────────────────────────────────────
            var token = _signer.Sign(session);
            await _localStorage.SetItemAsStringAsync(SessionKey, token);

            NotifyAuthenticationStateChanged(Task.FromResult(BuildState(session)));
        }

        public async Task RefreshActivity()
        {
            try
            {
                if (_cachedSession == null) return;

                _cachedSession.LastActivity = DateTime.Now;

                // Refirmar con el timestamp actualizado
                var token = _signer.Sign(_cachedSession);
                await _localStorage.SetItemAsStringAsync(SessionKey, token);
            }
            catch { /* silencioso */ }
        }

        public async Task Logout()
        {
            _cachedSession = null;
            await _localStorage.RemoveItemAsync(SessionKey);
            NotifyAuthenticationStateChanged(Task.FromResult(Anonymous()));
        }

        // ── helpers privados ──────────────────────────────────────────────────
        private static AuthenticationState? ValidateAndBuild(UserSession session)
        {
            if (DateTime.Now - session.LastActivity > TimeSpan.FromMinutes(30))
                return null;
            return BuildState(session);
        }

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