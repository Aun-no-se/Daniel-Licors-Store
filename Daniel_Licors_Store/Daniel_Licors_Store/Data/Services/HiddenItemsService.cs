using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    /// <summary>
    /// Centraliza la búsqueda y reactivación de elementos "ocultos"
    /// (IsActive = false) que NO fueron eliminados de la base de datos:
    /// Productos, Combos y Promociones. Pensado para el panel de
    /// "Elementos Ocultos" del dashboard (Admin y Empleado).
    /// </summary>
    public class HiddenItemsService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public HiddenItemsService(ApplicationDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── LISTAR OCULTOS ──────────────────────────────────────────────

        public async Task<List<HiddenProductDto>> GetHiddenProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductStores)
                .Where(p => !p.IsActive)
                .OrderByDescending(p => p.ProductId)
                .Select(p => new HiddenProductDto
                {
                    ProductId = p.ProductId,
                    Name = p.Name,
                    ImageUrl = p.ImageUrl,
                    CategoryName = p.Category != null ? p.Category.Name : "Sin categoría",
                    Price = p.ProductStores
                        .Where(ps => ps.StoreId == AppStore.StoreId)
                        .Select(ps => ps.Price)
                        .FirstOrDefault(),
                    Stock = p.ProductStores
                        .Where(ps => ps.StoreId == AppStore.StoreId)
                        .Select(ps => ps.Stock)
                        .FirstOrDefault()
                })
                .ToListAsync();
        }

        public async Task<List<HiddenComboDto>> GetHiddenCombosAsync()
        {
            return await _context.Combos
                .Include(c => c.ComboProducts)
                .Where(c => !c.IsActive)
                .OrderByDescending(c => c.ComboId)
                .Select(c => new HiddenComboDto
                {
                    ComboId = c.ComboId,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl,
                    ComboPrice = c.ComboPrice,
                    ProductCount = c.ComboProducts.Count
                })
                .ToListAsync();
        }

        public async Task<List<HiddenPromotionDto>> GetHiddenPromotionsAsync()
        {
            return await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Where(p => !p.IsActive)
                .OrderByDescending(p => p.PromotionId)
                .Select(p => new HiddenPromotionDto
                {
                    PromotionId = p.PromotionId,
                    Title = p.Title,
                    ImageUrl = p.ImageUrl,
                    PromotionType = p.PromotionType,
                    EndDate = p.EndDate,
                    ProductCount = p.PromotionProducts.Count
                })
                .ToListAsync();
        }

        // ── REACTIVAR (volver a mostrar) ────────────────────────────────

        public async Task<(bool Ok, string Error)> RestoreProductAsync(int productId, string? userId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return (false, "Producto no encontrado.");

            product.IsActive = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "Reactivó producto (visible de nuevo)", "Product", productId.ToString());
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> RestoreComboAsync(int comboId, string? userId)
        {
            var combo = await _context.Combos.FindAsync(comboId);
            if (combo == null) return (false, "Combo no encontrado.");

            combo.IsActive = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "Reactivó combo (visible de nuevo)", "Combo", comboId.ToString());
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> RestorePromotionAsync(int promotionId, string? userId)
        {
            var promo = await _context.Promotions.FindAsync(promotionId);
            if (promo == null) return (false, "Promoción no encontrada.");

            promo.IsActive = true;
            await _context.SaveChangesAsync();
            await _audit.LogAsync(userId, "Reactivó promoción (visible de nuevo)", "Promotion", promotionId.ToString());
            return (true, string.Empty);
        }
    }
}
