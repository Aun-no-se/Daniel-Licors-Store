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
        // Tamaño limpio + que no exista ya la misma bebida con el mismo tamaño
        var size = ProductSizeCatalog.NormalizeLabel(model.Size);
        var sizeMl = model.SizeMl ?? ProductSizeCatalog.TryParseMl(size);
        await EnsureNoDuplicateAsync(model.Name, model.CategoryId, model.BrandId, size, excludeProductId: null);

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
                AgingYears = model.AgingYears,
                Size = size,
                SizeMl = sizeMl
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
            Size = product.Size,
            SizeMl = product.SizeMl,
            Price = store?.Price ?? 0,
            Stock = store?.Stock ?? 0,
            IsPremium = product.IsPremium,
            IsActive = product.IsActive,
            ImageUrl = product.ImageUrl
        };
    }

    public async Task UpdateProductAsync(ProductEditModel model, string? userId = null)
    {
        var size = ProductSizeCatalog.NormalizeLabel(model.Size);
        var sizeMl = model.SizeMl ?? ProductSizeCatalog.TryParseMl(size);
        await EnsureNoDuplicateAsync(model.Name, model.CategoryId, model.BrandId, size, excludeProductId: model.ProductId);

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
            product.Size = size;
            product.SizeMl = sizeMl;
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

    // NOTA: Esto ya NO borra el producto de la base de datos.
    // Solo lo oculta (IsActive = false) para que no aparezca en catálogo,
    // carrito, combos ni promociones. Se puede reactivar desde el panel
    // de "Elementos Ocultos" en el Dashboard (Admin/Empleado).
    public async Task DeleteProductAsync(int productId, string? userId = null)
    {
        try
        {
            var product = await _context.Products
                .Include(p => p.ProductStores)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                throw new Exception("Producto no encontrado");

            product.IsActive = false;

            // También lo marcamos como no disponible en tienda para que
            // no se pueda seguir vendiendo mientras está oculto.
            var store = product.ProductStores?.FirstOrDefault(ps => ps.StoreId == AppStore.StoreId);
            if (store != null)
                store.IsAvailable = false;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(userId, "Ocultó producto", "Product", productId.ToString());
        }
        catch (Exception ex)
        {
            throw new Exception("Error ocultando producto: " + (ex.InnerException?.Message ?? ex.Message));
        }
    }

    // Sobrecarga sin userId para compatibilidad
    public async Task DeleteProductAsync(int productId)
        => await DeleteProductAsync(productId, null);

    // Cada tamaño es una bebida aparte, pero no puede haber dos iguales: mismo nombre,
    // misma categoría, misma marca y el mismo tamaño (ej. dos "Corona 355 ml").
    // Corona 355 ml y Corona 710 ml sí pueden convivir, cada una con su precio y stock.
    private async Task EnsureNoDuplicateAsync(string name, int categoryId, int? brandId, string? size, int? excludeProductId)
    {
        var cleanName = (name ?? string.Empty).Trim();

        var candidates = await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId
                        && p.BrandId == brandId
                        && p.Name == cleanName
                        && (excludeProductId == null || p.ProductId != excludeProductId))
            .Select(p => new { p.ProductId, p.Size, p.IsActive })
            .ToListAsync();

        var duplicate = candidates.FirstOrDefault(p =>
            string.Equals(p.Size ?? string.Empty, size ?? string.Empty, StringComparison.OrdinalIgnoreCase));

        if (duplicate == null) return;

        var sizeText = string.IsNullOrWhiteSpace(size) ? string.Empty : $" de {size}";

        throw new InvalidOperationException(duplicate.IsActive
            ? $"Ya existe «{cleanName}»{sizeText} en el catálogo. Si es otro tamaño, elígelo en los botones de tamaño."
            : $"Ya existe «{cleanName}»{sizeText}, pero está oculta. Reactívala desde «Elementos ocultos» en vez de crearla otra vez.");
    }

    public async Task<List<ProductDetailDto>> GetProductDetailsAsync()
    {
        return await _context.Products
            .Include(p => p.ProductStores)
            .Where(p => p.IsActive)
            .Select(p => new ProductDetailDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Size = p.Size,
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
