using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Components.Shared;
using Microsoft.EntityFrameworkCore;
using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Data.Services
{

    // ─── SERVICE ─────────────────────────────────────────────────────────────

    public class StockGeneralService
    {
        private readonly ApplicationDbContext _ctx;

        public StockGeneralService(ApplicationDbContext ctx) => _ctx = ctx;

        public async Task<StockSummary> GetSummaryAsync()
        {
            var storeId = AppStore.StoreId;

            var productos = await _ctx.ProductStores
                .Where(ps => ps.StoreId == storeId && ps.Product.IsActive)
                .ToListAsync();

            return new StockSummary
            {
                TotalProductos      = productos.Count,
                TotalUnidades       = productos.Sum(p => p.Stock),
                ProductosSinStock   = productos.Count(p => p.Stock == 0),
                TotalCombos         = await _ctx.Combos.CountAsync(c => c.IsActive),
                TotalPromociones    = await _ctx.Promotions.CountAsync(p => p.IsActive),
                CategoriasActivas   = await _ctx.Categories.CountAsync(c => c.IsActive)
            };
        }

        public async Task<List<StockProductRow>> GetProductsAsync(string? searchTerm = null, int? categoryId = null)
        {
            var storeId = AppStore.StoreId;

            var query = _ctx.ProductStores
                .Where(ps => ps.StoreId == storeId && ps.Product.IsActive)
                .Include(ps => ps.Product)
                    .ThenInclude(p => p.Category)
                .Include(ps => ps.Product)
                    .ThenInclude(p => p.Brand)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(ps => ps.Product.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(ps => ps.Product.Name.Contains(searchTerm) ||
                                          (ps.Product.Brand != null && ps.Product.Brand.Name.Contains(searchTerm)));

            return await query
                .OrderBy(ps => ps.Product.Category.Name)
                .ThenBy(ps => ps.Product.Name)
                .Select(ps => new StockProductRow
                {
                    ProductId   = ps.ProductId,
                    Name        = ps.Product.Name,
                    ImageUrl    = ps.Product.ImageUrl,
                    Category    = ps.Product.Category.Name,
                    CategoryId  = ps.Product.CategoryId,
                    Brand       = ps.Product.Brand != null ? ps.Product.Brand.Name : null,
                    Price       = ps.Price,
                    Stock       = ps.Stock,
                    IsAvailable = ps.IsAvailable,
                    IsPremium   = ps.Product.IsPremium,
                    AgingYears  = ps.Product.AgingYears
                })
                .ToListAsync();
        }

        public async Task<List<StockPromoRow>> GetPromotionsAsync()
        {
            var promos = await _ctx.Promotions
                .Where(p => p.IsActive)
                .Include(p => p.PromotionProducts)
                    .ThenInclude(pp => pp.Product)
                .OrderBy(p => p.EndDate)
                .ToListAsync();

            return promos.Select(p => new StockPromoRow
            {
                PromotionId    = p.PromotionId,
                Title          = p.Title,
                Description    = p.Description,
                PromotionType  = p.PromotionType ?? "",
                StartDate      = p.StartDate,
                EndDate        = p.EndDate,
                IsActive       = p.IsActive,
                Products       = p.PromotionProducts.Select(pp => new StockPromoProductRow
                {
                    Name         = pp.Product?.Name ?? "",
                    SpecialPrice = pp.SpecialPrice,
                    ImageUrl     = pp.Product?.ImageUrl
                }).ToList()
            }).ToList();
        }

        public async Task<List<StockComboRow>> GetCombosAsync()
        {
            var combos = await _ctx.Combos
                .Where(c => c.IsActive)
                .Include(c => c.ComboProducts)
                    .ThenInclude(cp => cp.Product)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return combos.Select(c => new StockComboRow
            {
                ComboId      = c.ComboId,
                Name         = c.Name,
                Description  = c.Description,
                ComboPrice   = c.ComboPrice,
                ImageUrl     = c.ImageUrl,
                EndDate      = c.EndDate,
                IsActive     = c.IsActive,
                ProductNames = c.ComboProducts.Select(cp => cp.Product?.Name ?? "").ToList()
            }).ToList();
        }

        public async Task<List<(string Name, int Id)>> GetCategoriesAsync()
        {
            return await _ctx.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new ValueTuple<string, int>(c.Name, c.CategoryId))
                .ToListAsync();
        }
    }
}
