using Microsoft.EntityFrameworkCore;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Data.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly IRepository<Category> _categoryRepo;
        private readonly IRepository<Brand> _brandRepo;
        private readonly IRepository<Product> _productRepo;

        public CatalogService(
            IRepository<Category> categoryRepo,
            IRepository<Brand> brandRepo,
            IRepository<Product> productRepo)
        {
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
            var products = await _productRepo.Query()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return products.Select(Map).ToList();
        }

        // =========================
        // PRODUCTS BY CATEGORY
        // =========================
        public async Task<List<ProductResponseModel>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _productRepo.Query()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.IsActive && p.CategoryId == categoryId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

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

        public async Task<bool> CreateProductAsync(ProductCreateModel model)
        {

            Console.WriteLine("🔥 CREATE PRODUCT CALLED: " + DateTime.Now.ToString("HH:mm:ss.fff"));
            try
            {
                var newProduct = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    BrandId = model.BrandId,
                    IsPremium = model.IsPremium,
                    IsActive = model.IsActive,
                    AgingYears = model.AgingYears,
                    ImageUrl = model.ImageFileName,
                    CreatedAt = DateTime.Now,
                    ProductStores = new List<ProductStore>
            {
                new ProductStore
                {
                    StoreId = 1,
                    Price = model.Price,
                    Stock = model.Stock,
                    IsAvailable = true
                }
            }
                };

                await _productRepo.AddAsync(newProduct); // 🔥 UNA SOLA VEZ

                return true;
            }
            catch (Exception ex)
            {
                // si quieres, log aquí
                return false;
            }
        }
    }
}