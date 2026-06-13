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
            if (string.IsNullOrWhiteSpace(session.UserId))
                throw new Exception("UserId no puede ser vacío al autenticar");

            await _localStorage.SetItemAsync("userSession", session);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, session.UserId),
        new Claim(ClaimTypes.Name, session.Email)
    };

            foreach (var role in session.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
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
