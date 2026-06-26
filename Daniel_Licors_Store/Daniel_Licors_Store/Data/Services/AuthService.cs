using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Users.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Daniel_Licors_Store.Services.Auth;

public interface IAuthService
{
    Task RegisterAsync(RegisterModel model);
    Task CreateUserByAdminAsync(AdminRegisterModel model);
    Task<(User? User, List<string> Roles)> ValidateLoginAsync(LoginModel model);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;
    private readonly PasswordHasher<User> _hasher = new();

    // Regex de email simple pero efectivo
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public AuthService(ApplicationDbContext context, AuditService audit)
    {
        _context = context;
        _audit = audit;
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
    // VALIDAR LOGIN — devuelve usuario + roles para no repetir query
    // ─────────────────────────────────────────────────────────────────
    public async Task<(User? User, List<string> Roles)> ValidateLoginAsync(LoginModel model)
    {
        var email = model.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(model.Password))
            return (null, new());

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return (null, new());

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (result == PasswordVerificationResult.Failed)
            return (null, new());

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        await _audit.LogAsync(user.UserId, "Inició sesión", "User", user.UserId);

        return (user, roles);
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
