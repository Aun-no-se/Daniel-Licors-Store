using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace Daniel_Licors_Store.Data.Services
{
    public class CustomAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;

        public CustomAuthStateProvider(ILocalStorageService localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var userSession = await _localStorage.GetItemAsync<UserSession>("userSession");

            if (userSession == null)
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userSession.UserId),
        new Claim(ClaimTypes.Name, userSession.Email)
    };

            foreach (var role in userSession.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "customAuth");
            var user = new ClaimsPrincipal(identity);

            return new AuthenticationState(user);
        }

        public async Task MarkUserAsAuthenticated(UserSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            // Guardar en localStorage
            await _localStorage.SetItemAsync("userSession", session);

            // Construir claims
            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(session.UserId))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, session.UserId));

            if (!string.IsNullOrWhiteSpace(session.Email))
                claims.Add(new Claim(ClaimTypes.Name, session.Email));

            if (session.Roles != null)
            {
                foreach (var role in session.Roles.Where(r => !string.IsNullOrWhiteSpace(r)))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            var identity = new ClaimsIdentity(claims, "customAuth");
            var user = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(
                Task.FromResult(new AuthenticationState(user))
            );
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("userSession");
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
