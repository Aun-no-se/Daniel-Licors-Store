using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;


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
            throw new Exception("Error creando producto: " + ex.Message);
        }
    }
}