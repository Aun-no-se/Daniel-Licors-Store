using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Users.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Daniel_Licors_Store.Services.Auth;

/// <summary>
/// Resultado de un intento de login. Nunca se lanza como excepción —
/// el bloqueo de cuenta y las credenciales inválidas son resultados
/// esperados del negocio, no errores excepcionales, así que viajan
/// como datos normales de retorno.
/// </summary>
public class LoginResult
{
    public User? User { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsLocked { get; set; }
    public string? ErrorMessage { get; set; }

    public static LoginResult Fail(string message) => new() { ErrorMessage = message };
    public static LoginResult Locked(string message) => new() { IsLocked = true, ErrorMessage = message };
    public static LoginResult Ok(User user, List<string> roles) => new() { User = user, Roles = roles };
}

public interface IAuthService
{
    Task RegisterAsync(RegisterModel model);
    Task CreateUserByAdminAsync(AdminRegisterModel model);
    Task<LoginResult> ValidateLoginAsync(LoginModel model);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly PasswordHasher<User> _hasher = new();

    // ── Política de bloqueo por fuerza bruta ─────────────────────────
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    // Hash "señuelo" fijo, calculado una sola vez al construir el servicio.
    // Se usa cuando el email NO existe, para que ValidateLoginAsync tarde
    // aproximadamente lo mismo con email real o inventado (evita que un
    // atacante detecte, por tiempo de respuesta, qué correos existen).
    private readonly string _dummyHash;

    // Regex de email simple pero efectivo
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AuthService(ApplicationDbContext context, AuditService audit)
    {
        _context = context;
        _audit = audit;
        _dummyHash = _hasher.HashPassword(new User(), "Dummy$NoExiste123");
    }

    // ─────────────────────────────────────────────────────────────────
    // REGISTRO DE CLIENTE
    // ─────────────────────────────────────────────────────────────────
    public async Task RegisterAsync(RegisterModel model)
    {
        // ── Validaciones de entrada ──────────────────────────────────
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new Exception("El nombre es requerido.");

        if (string.IsNullOrWhiteSpace(model.LastName))
            throw new Exception("El apellido es requerido.");

        if (model.Age < 18 || model.Age > 120)
            throw new Exception("Debes tener al menos 18 años para registrarte.");

        var email = model.Email?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            throw new Exception("El correo electrónico no es válido.");

        ValidatePassword(model.Password);

        if (model.Password != model.ConfirmPassword)
            throw new Exception("Las contraseñas no coinciden.");

        // ── Unicidad de email ────────────────────────────────────────
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new Exception("Este correo ya está registrado.");

        var roleCliente = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Cliente")
            ?? throw new Exception("Rol Cliente no configurado en el sistema.");

        // ── Crear usuario ────────────────────────────────────────────
        var user = new User
        {
            FullName = model.Name.Trim(),
            LastName = model.LastName.Trim(),
            Age = model.Age,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = roleCliente.RoleId,
            AssignedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        await _audit.LogAsync(user.UserId, "Registro de cuenta", "User", user.UserId);
    }

    // ─────────────────────────────────────────────────────────────────
    // ADMIN CREA USUARIOS (Empleado / Cliente / Admin)
    // ─────────────────────────────────────────────────────────────────
    public async Task CreateUserByAdminAsync(AdminRegisterModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            throw new Exception("El nombre es requerido.");

        var email = model.Email?.Trim().ToLower();
        if (string.IsNullOrWhiteSpace(email) || !EmailRegex.IsMatch(email))
            throw new Exception("El correo electrónico no es válido.");

        ValidatePassword(model.Password);

        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new Exception("Este correo ya está registrado.");

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == model.RoleId)
            ?? throw new Exception("Rol inválido.");

        var user = new User
        {
            FullName = model.Name.Trim(),
            LastName = model.LastName?.Trim(),
            Age = model.Age,
            Email = email,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        await _audit.LogAsync(user.UserId, $"Cuenta creada por admin — Rol: {role.Name}", "User", user.UserId);
    }

    // ─────────────────────────────────────────────────────────────────
    // VALIDAR LOGIN — bloqueo por intentos fallidos y mitigación de
    // enumeración de usuarios. NUNCA lanza excepciones para esto: todo
    // viaja en el LoginResult devuelto (por eso Visual Studio ya no
    // pausa el debugger con esto).
    // ─────────────────────────────────────────────────────────────────
    public async Task<LoginResult> ValidateLoginAsync(LoginModel model)
    {
        var email = model.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
            return LoginResult.Fail("Por favor ingresa tu correo y contraseña.");

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            // El correo no existe (o no tiene password). Igual corremos un
            // hash "señuelo" para que esta rama tarde aprox. lo mismo que
            // la rama de abajo — así el tiempo de respuesta no delata
            // si el correo está registrado o no.
            _hasher.VerifyHashedPassword(new User(), _dummyHash, model.Password);
            return LoginResult.Fail("El correo o la contraseña son incorrectos.");
        }

        // ── ¿Cuenta bloqueada por intentos fallidos previos? ──────────
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now)
        {
            var minutesLeft = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.Now).TotalMinutes);
            await _audit.LogAsync(user.UserId, "Intento de login rechazado: cuenta bloqueada", "User", user.UserId);
            return LoginResult.Locked(
                $"Cuenta bloqueada temporalmente por múltiples intentos fallidos. Intenta de nuevo en {minutesLeft} minuto(s).");
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.Now.AddMinutes(LockoutMinutes);
                user.FailedLoginAttempts = 0; // el contador se reinicia; ya quedó bloqueada
                await _context.SaveChangesAsync();

                await _audit.LogAsync(user.UserId,
                    $"Cuenta bloqueada {LockoutMinutes} min tras {MaxFailedAttempts} intentos fallidos consecutivos",
                    "User", user.UserId);

                return LoginResult.Locked(
                    $"Demasiados intentos fallidos. Tu cuenta ha sido bloqueada por {LockoutMinutes} minutos.");
            }

            await _context.SaveChangesAsync();
            await _audit.LogAsync(user.UserId,
                $"Intento de login fallido ({user.FailedLoginAttempts}/{MaxFailedAttempts})",
                "User", user.UserId);

            return LoginResult.Fail("El correo o la contraseña son incorrectos.");
        }

        // ── Login correcto: limpiar contador y cualquier bloqueo previo ──
        if (user.FailedLoginAttempts > 0 || user.LockoutEnd.HasValue)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        await _audit.LogAsync(user.UserId, "Inició sesión", "User", user.UserId);

        return LoginResult.Ok(user, roles);
    }

    // ─────────────────────────────────────────────────────────────────
    // VALIDACIÓN DE CONTRASEÑA CENTRALIZADA
    // ─────────────────────────────────────────────────────────────────
    private static void ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new Exception("La contraseña debe tener al menos 8 caracteres.");

        if (!password.Any(char.IsUpper))
            throw new Exception("La contraseña debe tener al menos una letra mayúscula.");

        if (!password.Any(char.IsDigit))
            throw new Exception("La contraseña debe contener al menos un número.");
    }
}
