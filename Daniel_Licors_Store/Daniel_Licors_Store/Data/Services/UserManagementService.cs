using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class UserManagementService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public UserManagementService(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        // ── 1. Resumen de cuentas por rol (tarjetas KPI) ────────────────────
        public async Task<List<UserRoleSummary>> GetRoleSummariesAsync()
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var rows = await ctx.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.IsActive)
                .Select(ur => new
                {
                    RoleName = ur.Role.Name,
                    ur.User.IsActive
                })
                .ToListAsync();

            return rows
                .GroupBy(r => r.RoleName)
                .Select(g => new UserRoleSummary
                {
                    RoleName = g.Key,
                    Total    = g.Count(),
                    Active   = g.Count(r => r.IsActive),
                    Inactive = g.Count(r => !r.IsActive)
                })
                .OrderBy(r => r.RoleName)
                .ToList();
        }

        // ── 2. Lista completa de usuarios con actividad ────────────────────
        public async Task<List<UserDetailDto>> GetAllUsersAsync(UserFilterModel filter)
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            // Traer todos los usuarios con su rol
            var query = ctx.UserRoles
                .Include(ur => ur.User)
                    .ThenInclude(u => u.Orders)
                .Include(ur => ur.Role)
                .AsQueryable();

            // Filtro por rol
            if (!string.IsNullOrWhiteSpace(filter.RoleFilter))
                query = query.Where(ur => ur.Role.Name == filter.RoleFilter);

            // Filtro por estado
            if (filter.StatusFilter == "active")
                query = query.Where(ur => ur.User.IsActive);
            else if (filter.StatusFilter == "inactive")
                query = query.Where(ur => !ur.User.IsActive);

            // Búsqueda por texto
            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var txt = filter.SearchText.ToLower();
                query = query.Where(ur =>
                    ur.User.FullName.ToLower().Contains(txt) ||
                    ur.User.LastName.ToLower().Contains(txt) ||
                    ur.User.Email.ToLower().Contains(txt));
            }

            var userRoles = await query.ToListAsync();

            var result = new List<UserDetailDto>();

            foreach (var ur in userRoles)
            {
                // Última actividad y total de acciones desde AuditLogs
                var lastLog = await ctx.AuditLogs
                    .Where(a => a.UserId == ur.User.UserId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                var totalActions = await ctx.AuditLogs
                    .CountAsync(a => a.UserId == ur.User.UserId);

                result.Add(new UserDetailDto
                {
                    UserId         = ur.User.UserId,
                    FullName       = ur.User.FullName,
                    LastName       = ur.User.LastName,
                    Email          = ur.User.Email,
                    Age            = ur.User.Age,
                    IsActive       = ur.User.IsActive,
                    CreatedAt      = ur.User.CreatedAt,
                    RoleName       = ur.Role.Name,
                    AssignedAt     = ur.AssignedAt,
                    TotalOrders    = ur.User.Orders.Count,
                    TotalActions   = totalActions,
                    LastAction     = lastLog?.Action,
                    LastEntity     = lastLog?.Entity,
                    LastEntityId   = lastLog?.EntityId,
                    LastActivityAt = lastLog?.CreatedAt
                });
            }

            // Ordenamiento
            result = filter.SortBy switch
            {
                "Name"    => result.OrderBy(r => r.FullName).ToList(),
                "Created" => result.OrderByDescending(r => r.CreatedAt).ToList(),
                _         => result.OrderByDescending(r => r.LastActivityAt ?? DateTime.MinValue).ToList()
            };

            return result;
        }

        // ── 3. Historial de auditoría de un usuario específico ─────────────
        public async Task<List<AuditLogDto>> GetUserAuditLogsAsync(string userId, int take = 50)
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var logs = await ctx.AuditLogs
                .Include(a => a.User)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();

            // Obtener el rol del usuario
            var roleName = await ctx.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role.Name)
                .FirstOrDefaultAsync() ?? "Sin Rol";

            return logs.Select(l => new AuditLogDto
            {
                AuditLogId = l.AuditLogId,
                UserId     = l.UserId ?? string.Empty,
                UserName   = l.User?.FullName ?? "Desconocido",
                RoleName   = roleName,
                Action     = l.Action,
                Entity     = l.Entity,
                EntityId   = l.EntityId,
                CreatedAt  = l.CreatedAt
            }).ToList();
        }

        // ── 4. Feed global de auditoría (todas las cuentas) ───────────────
        public async Task<List<AuditLogDto>> GetGlobalAuditFeedAsync(int take = 100)
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var logs = await ctx.AuditLogs
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .Take(take)
                .ToListAsync();

            // Pre-cargar roles de los usuarios que aparecen
            var userIds = logs.Where(l => l.UserId != null)
                              .Select(l => l.UserId!)
                              .Distinct()
                              .ToList();

            var roleMap = await ctx.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => userIds.Contains(ur.UserId))
                .ToDictionaryAsync(ur => ur.UserId, ur => ur.Role.Name);

            return logs.Select(l => new AuditLogDto
            {
                AuditLogId = l.AuditLogId,
                UserId     = l.UserId ?? string.Empty,
                UserName   = l.User?.FullName ?? "Desconocido",
                RoleName   = l.UserId != null && roleMap.TryGetValue(l.UserId, out var r) ? r : "–",
                Action     = l.Action,
                Entity     = l.Entity,
                EntityId   = l.EntityId,
                CreatedAt  = l.CreatedAt
            }).ToList();
        }

        // ── 5. Activar / Desactivar cuenta ────────────────────────────────
        public async Task<bool> ToggleUserStatusAsync(string userId, string performedByUserId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var user = await ctx.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = !user.IsActive;

            var action = user.IsActive ? "Cuenta Activada" : "Cuenta Desactivada";

            ctx.AuditLogs.Add(new AuditLog
            {
                UserId    = performedByUserId,
                Action    = action,
                Entity    = "User",
                EntityId  = userId,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            return true;
        }

        // ── 6. Registrar acción de auditoría (helper para otros servicios) ─
        public async Task LogAsync(string? userId, string action, string entity, string? entityId = null)
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            ctx.AuditLogs.Add(new AuditLog
            {
                UserId    = userId,
                Action    = action,
                Entity    = entity,
                EntityId  = entityId,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
        }
    }
}
