using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Users.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Services.Auth;

public interface IAuthService
{
    Task RegisterAsync(RegisterModel model);
    Task CreateUserByAdminAsync(AdminRegisterModel model);
    Task<User?> ValidateLoginAsync(LoginModel model);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================
    // REGISTER CLIENTE
    // =========================
    public async Task RegisterAsync(RegisterModel model)
    {
        if (model.Password != model.ConfirmPassword)
            throw new Exception("Las contraseñas no coinciden");

        var email = model.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email inválido");

        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new Exception("Este correo ya está registrado");

        var roleCliente = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "Cliente");

        if (roleCliente == null)
            throw new Exception("Rol Cliente no existe");

        var user = new User
        {
            FullName = model.Name?.Trim(),
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
            RoleId = roleCliente.RoleId,
            AssignedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();
    }

    // =========================
    // ADMIN CREA USUARIOS
    // =========================
    public async Task CreateUserByAdminAsync(AdminRegisterModel model)
    {
        var email = model.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email))
            throw new Exception("Email inválido");

        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
            throw new Exception("Este correo ya está registrado");

        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == model.RoleId);

        if (role == null)
            throw new Exception("Rol inválido");

        var user = new User
        {
            FullName = model.Name?.Trim(),
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
    }

    // =========================
    // LOGIN VALIDACIÓN
    // =========================
    public async Task<User?> ValidateLoginAsync(LoginModel model)
    {
        var email = model.Email?.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email))
            return null;

        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return null;

        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            model.Password
        );

        return result == PasswordVerificationResult.Success
            ? user
            : null;
    }
}