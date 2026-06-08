using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Promotion;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services;

public class PromotionService : IPromotionService
{
    private readonly ApplicationDbContext _context;

    public PromotionService(ApplicationDbContext context)
    {
        _context = context;
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    // ============================================================
    // LISTAR TODAS (ADMIN)
    // ============================================================
    public async Task<List<PromotionAdminDto>> GetAdminPromotionsAsync()
    {
        var promos = await _context.Promotions
            .Include(p => p.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return promos.Select(p => new PromotionAdminDto
        {
            PromotionId    = p.PromotionId,
            Title          = p.Title,
            Description    = p.Description,
            PromotionType  = p.PromotionType,
            StartDate      = p.StartDate,
            EndDate        = p.EndDate,
            IsActive       = p.IsActive,
            ProductCount   = p.PromotionProducts.Count,
            ProductsSummary = string.Join(", ",
                p.PromotionProducts.Select(pp => pp.Product?.Name ?? "").Where(n => n != ""))
        }).ToList();
    }

    // ============================================================
    // OBTENER UNO (para editar)
    // ============================================================
    public async Task<PromotionAdminDto?> GetPromotionByIdAsync(int promotionId)
    {
        var p = await _context.Promotions
            .Include(x => x.PromotionProducts)
                .ThenInclude(pp => pp.Product)
            .FirstOrDefaultAsync(x => x.PromotionId == promotionId);

        if (p == null) return null;

        return new PromotionAdminDto
        {
            PromotionId    = p.PromotionId,
            Title          = p.Title,
            Description    = p.Description,
            PromotionType  = p.PromotionType,
            StartDate      = p.StartDate,
            EndDate        = p.EndDate,
            IsActive       = p.IsActive,
            ProductCount   = p.PromotionProducts.Count,
            ProductsSummary = string.Join(", ",
                p.PromotionProducts.Select(pp => pp.Product?.Name ?? ""))
        };
    }

    // ============================================================
    // CREAR
    // ============================================================
    public async Task<bool> CreatePromotionAsync(PromotionCreateModel model)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var promo = new Promotion
            {
                Title         = model.Title.Trim(),
                Description   = model.Description?.Trim(),
                StartDate     = model.StartDate,
                EndDate       = model.EndDate,
                PromotionType = model.PromotionType,
                ImageUrl      = model.ImageUrl,
                IsActive      = model.IsActive
            };

            _context.Promotions.Add(promo);
            await _context.SaveChangesAsync();

            foreach (var line in model.Products)
            {
                _context.PromotionProducts.Add(new PromotionProduct
                {
                    PromotionId        = promo.PromotionId,
                    ProductId          = line.ProductId,
                    SpecialPrice       = line.SpecialPrice,
                    DiscountPercentage = line.DiscountPercentage,
                    RequiredQuantity   = line.RequiredQuantity,
                    PayQuantity        = line.PayQuantity
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================
    // ACTUALIZAR
    // ============================================================
    public async Task UpdatePromotionAsync(int promotionId, PromotionCreateModel model)
    {
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            var promo = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId)
                ?? throw new Exception("Promoción no encontrada");

            promo.Title         = model.Title.Trim();
            promo.Description   = model.Description?.Trim();
            promo.StartDate     = model.StartDate;
            promo.EndDate       = model.EndDate;
            promo.PromotionType = model.PromotionType;
            promo.IsActive      = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.ImageUrl))
                promo.ImageUrl = model.ImageUrl;

            // Reemplazar líneas de productos
            _context.PromotionProducts.RemoveRange(promo.PromotionProducts);
            await _context.SaveChangesAsync();

            foreach (var line in model.Products)
            {
                _context.PromotionProducts.Add(new PromotionProduct
                {
                    PromotionId        = promo.PromotionId,
                    ProductId          = line.ProductId,
                    SpecialPrice       = line.SpecialPrice,
                    DiscountPercentage = line.DiscountPercentage,
                    RequiredQuantity   = line.RequiredQuantity,
                    PayQuantity        = line.PayQuantity
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ============================================================
    // ELIMINAR
    // ============================================================
    public async Task DeletePromotionAsync(int promotionId)
    {
        var promo = await _context.Promotions
            .Include(p => p.PromotionProducts)
            .FirstOrDefaultAsync(p => p.PromotionId == promotionId)
            ?? throw new Exception("Promoción no encontrada");

        _context.PromotionProducts.RemoveRange(promo.PromotionProducts);
        _context.Promotions.Remove(promo);
        await _context.SaveChangesAsync();
    }
}
