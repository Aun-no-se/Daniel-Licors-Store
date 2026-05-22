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
        // CENTRAL MAPPER
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

                // agrega esto si lo tienes en DTO
                Price = p.ProductStores.FirstOrDefault()?.Price ?? 0,
                Stock = p.ProductStores.FirstOrDefault()?.Stock ?? 0
            };
        }

        public async Task<ProductDetailDto?> GetProductDetailAsync(int productId)
        {
            // Usamos directamente el _context para poder incluir de manera limpia las relaciones
            var p = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.ProductStores) // 👈 CRUCIAL para el precio y el stock
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.IsActive);

            if (p == null) return null;

            return MapDetail(p);
        }

        public async Task<List<ComboDto>> GetCombosByProductAsync(int productId)
        {
            var combos = await _context.ComboProducts
                .Where(cp => cp.ProductId == productId)
                .Include(cp => cp.Combo)
                .Select(cp => new ComboDto
                {
                    ComboId = cp.ComboId,
                    Name = cp.Combo.Name,
                    Description = cp.Combo.Description,
                    ComboPrice = cp.Combo.ComboPrice
                })
                .ToListAsync();

            return combos;
        }

        public async Task<List<PromotionDto>> GetPromotionsByProductAsync(int productId)
        {
            var promos = await _context.PromotionProducts
                .Where(pp => pp.ProductId == productId)
                .Include(pp => pp.Promotion)
                .Select(pp => new PromotionDto
                {
                    PromotionId = pp.PromotionId,
                    Title = pp.Promotion.Title,
                    Description = pp.Promotion.Description
                })
                .ToListAsync();

            return promos;
        }
    }
}