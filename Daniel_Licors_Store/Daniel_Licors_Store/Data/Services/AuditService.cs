using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    /// <summary>
    /// Servicio centralizado de auditoría.
    /// Registra acciones en AuditLogs. Usa IDbContextFactory propio
    /// para no interferir con el DbContext scoped de otros servicios.
    /// </summary>
    public class AuditService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _factory;

        public AuditService(IDbContextFactory<ApplicationDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Registra una acción. Si falla, solo escribe en consola —
        /// nunca interrumpe la operación principal.
        /// </summary>
        public async Task LogAsync(string? userId, string action, string entity, string? entityId = null)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                await using var ctx = await _factory.CreateDbContextAsync();
                ctx.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    Action = action,
                    Entity = entity,
                    EntityId = entityId,
                    CreatedAt = DateTime.Now
                });
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuditService] Error registrando log: {ex.Message}");
            }
        }
    }
}
