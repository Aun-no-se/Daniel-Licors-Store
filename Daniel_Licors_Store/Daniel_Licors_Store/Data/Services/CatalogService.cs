using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class CatalogService : ICatalogService
    {
        // Usamos IDbContextFactory en lugar de ApplicationDbContext directo.
        // Esto evita el TaskCanceledException cuando Blazor Server hace el
        // pre-render y cancela el scope antes de que terminen las queries.
        private readonly IDbContextFactory<ApplicationDbContext> _factory;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Brand> _brandRepo;

        public CatalogService(
            IDbContextFactory<ApplicationDbContext> factory,
            IRepository<Category> categoryRepo,
            IRepository<Brand> brandRepo)
        {
            _factory = factory;
            _categoryRepo = categoryRepo;
            _brandRepo = brandRepo;
        }

        // Mismas listas (y mismo orden A→Z) que ve el admin en "Categorías y Marcas":
        // lo que se registra en el panel de control es lo que aparece en los
        // desplegables del formulario de bebidas.
        public async Task<List<Category>> GetCategoriesAsync()
            => (await _categoryRepo.FindAsync(c => c.IsActive))
                .OrderBy(c => c.Name)
                .ToList();

        public async Task<List<Brand>> GetBrandsAsync()
            => (await _brandRepo.FindAsync(b => b.IsActive))
                .OrderBy(b => b.Name)
                .ToList();

        public async Task<List<ProductResponseModel>> GetProductsAsync()
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var products = await ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductStores)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return products.Select(Map).ToList();
        }

        public async Task<List<ProductResponseModel>> GetProductsByCategoryAsync(int categoryId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var products = await ctx.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductStores)
                .Where(p => p.IsActive && p.CategoryId == categoryId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
            return products.Select(Map).ToList();
        }

        // Arma las secciones del index directamente desde la base de datos: UNA por
        // cada categoría ACTIVA, aunque todavía no tenga productos. Las marcas NO
        // generan sección (cada bebida ya aparece en la de su categoría; si no, se
        // vería repetida). Las marcas quedan para filtrar más adelante.
        public async Task<List<CatalogSectionModel>> GetCatalogSectionsAsync()
        {
            await using var ctx = await _factory.CreateDbContextAsync();

            var categories = await ctx.Categories.AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            var products = await ctx.Products.AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductStores)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var sections = new List<CatalogSectionModel>();

            foreach (var c in categories)
            {
                sections.Add(new CatalogSectionModel
                {
                    Kind = "categoria",
                    Id = c.CategoryId,
                    Title = c.Name,
                    Products = products.Where(p => p.CategoryId == c.CategoryId).Select(Map).ToList()
                });
            }

            return sections;
        }

        public async Task<ProductDetailDto?> GetProductDetailAsync(int productId)
        {
            await using var ctx = await _factory.CreateDbContextAsync();
            var p = await ctx.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.ProductStores)
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive);
            if (p == null) return null;

            var detail = MapDetail(p);

            // Otros tamaños de esta misma bebida (mismo nombre, categoría y marca)
            var sameDrink = await ctx.Products
                .AsNoTracking()
                .Include(x => x.ProductStores)
                .Where(x => x.IsActive
                            && x.CategoryId == p.CategoryId
                            && x.BrandId == p.BrandId
                            && x.Name == p.Name)
                .ToListAsync();

            if (sameDrink.Count > 1)
            {
                detail.Variants = sameDrink
                    .OrderBy(x => x.SizeMl ?? int.MaxValue)
                    .ThenBy(x => x.Size)
                    .Select(x =>
                    {
                        var vs = x.ProductStores?.FirstOrDefault(ps => ps.StoreId == 1)
                              ?? x.ProductStores?.FirstOrDefault();
                        return new ProductSizeVariantDto
                        {
                            ProductId = x.ProductId,
                            Size = x.Size,
                            Price = vs?.Price ?? 0,
                            Stock = vs?.Stock ?? 0,
                            IsCurrent = x.ProductId == p.ProductId
                        };
                    })
                    .ToList();
            }

            return detail;
        }

        public async Task<List<ComboDto>> GetCombosByProductAsync(int productId)
        {
            var now = DateTime.Now;
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.ComboProducts
                .Where(cp =>
                    cp.ProductId == productId &&
                    cp.Combo.IsActive &&
                    (cp.Combo.StartDate == null || cp.Combo.StartDate <= now) &&
                    (cp.Combo.EndDate == null || cp.Combo.EndDate >= now))
                .Include(cp => cp.Combo)
                .Select(cp => new ComboDto
                {
                    ComboId = cp.ComboId,
                    Name = cp.Combo.Name,
                    Description = cp.Combo.Description,
                    ComboPrice = cp.Combo.ComboPrice,
                    ImageUrl = cp.Combo.ImageUrl
                })
                .ToListAsync();
        }

        public async Task<List<PromotionDto>> GetPromotionsByProductAsync(int productId)
        {
            var now = DateTime.Now;
            await using var ctx = await _factory.CreateDbContextAsync();
            return await ctx.PromotionProducts
                .Where(pp =>
                    pp.ProductId == productId &&
                    pp.Promotion.IsActive &&
                    pp.Promotion.StartDate <= now &&
                    pp.Promotion.EndDate >= now)
                .Include(pp => pp.Promotion)
                .Select(pp => new PromotionDto
                {
                    PromotionId = pp.PromotionId,
                    Title = pp.Promotion.Title,
                    Description = pp.Promotion.Description,
                    PromotionType = pp.Promotion.PromotionType,
                    ImageUrl = pp.Promotion.ImageUrl,
                    StartDate = pp.Promotion.StartDate,
                    EndDate = pp.Promotion.EndDate,
                    SpecialPrice = pp.SpecialPrice,
                    DiscountPercentage = pp.DiscountPercentage,
                    RequiredQuantity = pp.RequiredQuantity,
                    PayQuantity = pp.PayQuantity
                })
                .ToListAsync();
        }

        // ── Mappers ──────────────────────────────────────────────────────────
        private static ProductResponseModel Map(Product p)
        {
            var store = p.ProductStores?.FirstOrDefault(ps => ps.StoreId == 1)
                     ?? p.ProductStores?.FirstOrDefault();
            return new ProductResponseModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category?.Name ?? "",
                Brand = p.Brand?.Name,
                ImageUrl = p.ImageUrl,
                IsPremium = p.IsPremium,
                Size = p.Size,
                IsActive = p.IsActive,
                Price = store?.Price ?? 0
            };
        }

        private static ProductDetailDto MapDetail(Product p)
        {
            var store = p.ProductStores?.FirstOrDefault(ps => ps.StoreId == 1)
                     ?? p.ProductStores?.FirstOrDefault();
            return new ProductDetailDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category?.Name ?? "",
                Brand = p.Brand?.Name,
                ImageUrl = p.ImageUrl,
                IsPremium = p.IsPremium,
                Size = p.Size,
                IsActive = p.IsActive,
                Price = store?.Price ?? 0,
                Stock = store?.Stock ?? 0
            };
        }
    }
}