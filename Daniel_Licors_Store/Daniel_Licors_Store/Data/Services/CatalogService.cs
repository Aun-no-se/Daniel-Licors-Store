using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Brand> _brandRepo;
        private readonly IRepository<Product> _productRepo;

        public CatalogService(
            ApplicationDbContext context,
            IRepository<Category> categoryRepo,
            IRepository<Brand> brandRepo,
            IRepository<Product> productRepo)
        {
            _context = context;
            _categoryRepo = categoryRepo;
            _brandRepo = brandRepo;
            _productRepo = productRepo;
        }

        public async Task<List<Category>> GetCategoriesAsync()
            => (await _categoryRepo.FindAsync(c => c.IsActive)).ToList();

        public async Task<List<Brand>> GetBrandsAsync()
            => (await _brandRepo.FindAsync(b => b.IsActive)).ToList();

        // =========================
        // PRODUCTS (HOME)
        // =========================
        public async Task<List<ProductResponseModel>> GetProductsAsync()
        {
            var products = (await _productRepo.GetAllAsync())
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return products.Select(Map).ToList();
        }

        // =========================
        // PRODUCTS BY CATEGORY
        // =========================
        public async Task<List<ProductResponseModel>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = (await _productRepo.GetAllAsync())
                .Where(p => p.IsActive && p.CategoryId == categoryId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            return products.Select(Map).ToList();
        }

        // =========================
        // PRODUCT DETAIL
        // =========================
        public async Task<ProductDetailDto?> GetProductDetailAsync(int productId)
        {
            var p = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.ProductStores)
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive);

            if (p == null)
                return null;

            return MapDetail(p);
        }

        // =========================
        // COMBOS BY PRODUCT
        // =========================
        public async Task<List<ComboDto>> GetCombosByProductAsync(int productId)
        {
            var now = DateTime.Now;

            return await _context.ComboProducts
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
                    ComboPrice = cp.Combo.ComboPrice
                })
                .ToListAsync();
        }

        // =========================
        // PROMOTIONS BY PRODUCT
        // Solo activas y dentro de vigencia
        // =========================
        public async Task<List<PromotionDto>> GetPromotionsByProductAsync(int productId)
        {
            var now = DateTime.Now;

            return await _context.PromotionProducts
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

        // =========================
        // MAPPERS
        // =========================
        private static ProductResponseModel Map(Product p)
        {
            return new ProductResponseModel
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category?.Name ?? "",
                Brand = p.Brand?.Name,
                ImageUrl = p.ImageUrl,
                IsPremium = p.IsPremium,
                IsActive = p.IsActive
            };
        }

        private static ProductDetailDto MapDetail(Product p)
        {
            var store = p.ProductStores?.FirstOrDefault();

            return new ProductDetailDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Category = p.Category?.Name ?? "",
                Brand = p.Brand?.Name,
                ImageUrl = p.ImageUrl,
                IsPremium = p.IsPremium,
                IsActive = p.IsActive,
                Price = store?.Price ?? 0,
                Stock = store?.Stock ?? 0
            };
        }
    }
}
