using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Microsoft.EntityFrameworkCore;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly AuditService _audit;

    public ProductService(ApplicationDbContext context, AuditService audit)
    {
        _context = context;
        _audit = audit;

        // Evita comportamientos raros en Blazor Server con tracking EF
        _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
    }

    public async Task<int> CreateProductAsync(ProductCreateModel model, string? userId = null)
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
                ImageUrl = string.IsNullOrWhiteSpace(model.ImageFileName)
                    ? null
                    : model.ImageFileName,
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
                StoreId = AppStore.StoreId,
                Price = model.Price,
                Stock = model.Stock,
                IsAvailable = model.Stock > 0
            };

            _context.ProductStores.Add(productStore);

            // =========================
            // 3. STOCK MOVEMENT
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
            await transaction.CommitAsync();

            await _audit.LogAsync(userId, "Creó producto", "Product", product.ProductId.ToString());

            return product.ProductId;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Error creando producto: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }

    // Sobrecarga sin userId para compatibilidad con código existente
    public async Task<int> CreateProductAsync(ProductCreateModel model)
        => await CreateProductAsync(model, null);

    public async Task<ProductEditModel?> GetProductForEditAsync(int productId)
    {
        var product = await _context.Products
            .Include(p => p.ProductStores)
            .FirstOrDefaultAsync(p => p.ProductId == productId);

        if (product == null)
            return null;

        var store = product.ProductStores?
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

    public async Task UpdateProductAsync(ProductEditModel model, string? userId = null)
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
            // 1. PRODUCTO
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
            var store = product.ProductStores?
                .FirstOrDefault(ps => ps.StoreId == AppStore.StoreId);

            if (store == null)
            {
                store = new ProductStore
                {
                    ProductId = product.ProductId,
                    StoreId = AppStore.StoreId,
                    Price = model.Price,
                    Stock = model.Stock,
                    IsAvailable = model.Stock > 0
                };

                _context.ProductStores.Add(store);
            }

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

            await _audit.LogAsync(userId, "Editó producto", "Product", model.ProductId.ToString());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Error actualizando producto: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }

    // Sobrecarga sin userId para compatibilidad
    public async Task UpdateProductAsync(ProductEditModel model)
        => await UpdateProductAsync(model, null);

    public async Task DeleteProductAsync(int productId, string? userId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var productStores = await _context.ProductStores
                .Where(ps => ps.ProductId == productId)
                .ToListAsync();

            _context.ProductStores.RemoveRange(productStores);

            var stockMovements = await _context.StockMovements
                .Where(sm => sm.ProductId == productId)
                .ToListAsync();

            _context.StockMovements.RemoveRange(stockMovements);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product != null)
                _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _audit.LogAsync(userId, "Eliminó producto", "Product", productId.ToString());
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Error eliminando producto: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }

    // Sobrecarga sin userId para compatibilidad
    public async Task DeleteProductAsync(int productId)
        => await DeleteProductAsync(productId, null);

    public async Task<List<ProductDetailDto>> GetProductDetailsAsync()
    {
        return await _context.Products
            .Include(p => p.ProductStores)
            .Where(p => p.IsActive)
            .Select(p => new ProductDetailDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.ProductStores
                    .Where(ps => ps.StoreId == AppStore.StoreId)
                    .Select(ps => ps.Price)
                    .FirstOrDefault(),
                Stock = p.ProductStores
                    .Where(ps => ps.StoreId == AppStore.StoreId)
                    .Select(ps => ps.Stock)
                    .FirstOrDefault(),
                ImageUrl = p.ImageUrl,
                IsActive = p.IsActive
            })
            .ToListAsync();
    }
}
