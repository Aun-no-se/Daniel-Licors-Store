using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace Daniel_Licors_Store.Services.Auth
{
    /// <summary>
    /// Handler de autenticación vacío (no-op).
    /// Solo existe para que UseAuthorization() pueda registrar un esquema
    /// y llamar a ChallengeAsync sin explotar.
    /// La autenticación real la maneja CustomAuthStateProvider con LocalStorage + HMAC.
    /// </summary>
    public class NoOpAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public NoOpAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        // Siempre devuelve NoResult — la identidad real viene del CustomAuthStateProvider
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            => Task.FromResult(AuthenticateResult.NoResult());

        // No hace nada en el challenge — evita el crash sin redirigir a ningún lado
        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
            => Task.CompletedTask;

        // No hace nada en el forbid
        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
            => Task.CompletedTask;
    }
}
