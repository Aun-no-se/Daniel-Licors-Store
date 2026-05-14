using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Microsoft.EntityFrameworkCore;


public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateProductAsync(ProductCreateModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // =========================
            // 1. PRODUCTO BASE
            // =========================
            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId,
                ImageUrl = model.ImageFileName,
                IsPremium = model.IsPremium,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                AgingYears = model.AgingYears
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // =========================
            // 2. INVENTARIO HAINA
            // =========================
            var productStore = new ProductStore
            {
                ProductId = product.ProductId,
                StoreId = AppStore.StoreId, // 👈 FIJO HAINA
                Price = model.Price,
                Stock = model.Stock,
                IsAvailable = model.Stock > 0
            };

            _context.ProductStores.Add(productStore);

            // =========================
            // 3. STOCK MOVEMENT (AUDITORÍA)
            // =========================
            var movement = new StockMovement
            {
                ProductId = product.ProductId,
                StoreId = AppStore.StoreId,
                Quantity = model.Stock,
                MovementType = "In",
                CreatedAt = DateTime.Now
            };

            _context.StockMovements.Add(movement);

            await _context.SaveChangesAsync();

            // =========================
            // COMMIT
            // =========================
            await transaction.CommitAsync();

            return product.ProductId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            var message = ex.InnerException?.Message ?? ex.Message;
            throw new Exception("Error creando producto: " + message);
        }
    }

    public async Task<ProductEditModel?> GetProductForEditAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return null;

        var store = product.ProductStores
            .FirstOrDefault(ps => ps.StoreId == AppStore.StoreId);

        return new ProductEditModel
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Description = product.Description,
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            AgingYears = product.AgingYears,
            Price = store?.Price ?? 0,
            Stock = store?.Stock ?? 0,
            IsPremium = product.IsPremium,
            IsActive = product.IsActive,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task UpdateProductAsync(ProductEditModel model)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var product = await _context.Products
                .Include(p => p.ProductStores)
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
                throw new Exception("Producto no encontrado");

            // =========================
            // 1. UPDATE PRODUCT
            // =========================
            product.Name = model.Name;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.AgingYears = model.AgingYears;
            product.IsPremium = model.IsPremium;
            product.IsActive = model.IsActive;
            product.ImageUrl = model.ImageUrl;

            // =========================
            // 2. INVENTARIO
            // =========================
            var store = product.ProductStores
                .FirstOrDefault(ps => ps.StoreId == AppStore.StoreId);

            if (store == null)
                throw new Exception("Inventario no encontrado para la tienda");

            var stockDiff = model.Stock - store.Stock;

            store.Price = model.Price;
            store.Stock = model.Stock;
            store.IsAvailable = model.Stock > 0;

            // =========================
            // 3. STOCK MOVEMENT
            // =========================
            if (stockDiff != 0)
            {
                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = product.ProductId,
                    StoreId = AppStore.StoreId,
                    Quantity = Math.Abs(stockDiff),
                    MovementType = stockDiff > 0 ? "In" : "Out",
                    CreatedAt = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteProductAsync(int productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return;

        product.IsActive = false;
        await _context.SaveChangesAsync();
    }
}