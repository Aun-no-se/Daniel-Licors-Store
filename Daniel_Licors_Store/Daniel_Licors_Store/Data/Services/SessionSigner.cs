using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Daniel_Licors_Store.Data.Services;

/// <summary>
/// Firma y verifica el payload de UserSession con HMAC-SHA256.
///
/// La clave YA NO vive en appsettings.json (ese archivo se commitea al
/// repo). Vive en:
///   - Desarrollo: User Secrets → dotnet user-secrets set "SessionSecurity:HmacKey" "..."
///   - Producción: variable de entorno SessionSecurity__HmacKey
///     (el doble guion bajo es la sintaxis de .NET para representar el
///     ":" anidado de configuración en nombres de variables de entorno)
///
/// IConfiguration combina ambas fuentes automáticamente — este archivo
/// no necesita saber de dónde vino el valor.
///
/// Si alguien edita el LocalStorage, la firma no coincide y la sesión
/// se rechaza completamente — el usuario queda como anónimo.
/// </summary>
public class SessionSigner
{
    private readonly byte[] _key;

    public SessionSigner(IConfiguration configuration)
    {
        var keyBase64 = configuration["SessionSecurity:HmacKey"];

        if (string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException(
                "SessionSecurity:HmacKey no está configurado. " +
                "En desarrollo: dotnet user-secrets set \"SessionSecurity:HmacKey\" \"<clave>\". " +
                "En producción: variable de entorno SessionSecurity__HmacKey.");

        _key = Convert.FromBase64String(keyBase64);
    }

    // ── FIRMAR ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Serializa el objeto a JSON, calcula HMAC-SHA256 y devuelve
    /// un token con formato:  base64(json).<base64(firma)>
    /// </summary>
    public string Sign<T>(T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var jsonB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var sig = ComputeHmac(jsonB64);

        return $"{jsonB64}.{sig}";
    }

    // ── VERIFICAR ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Verifica la firma y deserializa el payload.
    /// Devuelve null si el token está malformado o la firma no coincide.
    /// </summary>
    public T? Verify<T>(string token) where T : class
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return null;

        var jsonB64 = parts[0];
        var receivedSig = parts[1];
        var expectedSig = ComputeHmac(jsonB64);

        // Comparación en tiempo constante — evita ataques de timing
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(receivedSig),
                Convert.FromBase64String(expectedSig)))
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(jsonB64));
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    // ── PRIVADO ────────────────────────────────────────────────────────────────
    private string ComputeHmac(string data)
    {
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
}
