using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class ComboService : IComboService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditService _audit;

        public ComboService(ApplicationDbContext context, AuditService audit)
        {
            _context = context;
            _audit = audit;

            // Evita comportamientos raros en tracking (Blazor Server + EF)
            _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        }

        public async Task<bool> ValidateComboStockAsync(int comboId, int storeId, int quantity)
        {
            var comboProducts = await _context.ComboProducts
                .Where(cp => cp.ComboId == comboId)
                .ToListAsync();

            foreach (var item in comboProducts)
            {
                var productStore = await _context.ProductStores
                    .FirstOrDefaultAsync(ps =>
                        ps.ProductId == item.ProductId &&
                        ps.StoreId == storeId);

                if (productStore == null)
                    return false;

                var required = item.Quantity * quantity;

                if (productStore.Stock < required)
                    return false;
            }

            return true;
        }

        public async Task<bool> ConsumeComboStockAsync(int comboId, int storeId, int quantity, string? userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (!await ValidateComboStockAsync(comboId, storeId, quantity))
                    return false;

                var comboProducts = await _context.ComboProducts
                    .Where(cp => cp.ComboId == comboId)
                    .ToListAsync();

                foreach (var item in comboProducts)
                {
                    var productStore = await _context.ProductStores
                        .FirstOrDefaultAsync(ps =>
                            ps.ProductId == item.ProductId &&
                            ps.StoreId == storeId);

                    if (productStore == null)
                        throw new Exception("Inventario no encontrado para producto del combo");

                    var required = item.Quantity * quantity;

                    if (productStore.Stock < required)
                        throw new Exception("Stock insuficiente en producto del combo");

                    productStore.Stock -= required;

                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = item.ProductId,
                        StoreId = storeId,
                        Quantity = required,
                        MovementType = "Out",
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ComboDto>> GetCombosByProductAsync(int productId)
        {
            return await _context.ComboProducts
                .Where(cp => cp.ProductId == productId && cp.Combo.IsActive)
                .Select(cp => new ComboDto
                {
                    ComboId = cp.ComboId,
                    Name = cp.Combo.Name,
                    Description = cp.Combo.Description ?? "",
                    ComboPrice = cp.Combo.ComboPrice
                })
                .Distinct()
                .ToListAsync();
        }

        public async Task<ComboDetailDto?> GetComboDetailAsync(int comboId)
        {
            return await _context.Combos
                .Where(c => c.ComboId == comboId && c.IsActive)
                .Select(c => new ComboDetailDto
                {
                    ComboId = c.ComboId,
                    Name = c.Name,
                    Description = c.Description ?? "",
                    ComboPrice = c.ComboPrice,

                    ImageUrl = string.IsNullOrWhiteSpace(c.ImageUrl)
                        ? "/images/combo-default.png"
                        : c.ImageUrl,

                    Products = c.ComboProducts.Select(cp => new ComboProductDetailDto
                    {
                        ProductId = cp.ProductId,
                        ProductName = cp.Product.Name,
                        Description = cp.Product.Description ?? "",
                        ImageUrl = cp.Product.ImageUrl ?? "/images/default-product.png",

                        Price = cp.Product.ProductStores
                            .Select(ps => ps.Price)
                            .FirstOrDefault(),

                        Quantity = cp.Quantity
                    }).ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<ComboDto>> GetActiveCombosAsync()
        {
            return await _context.Combos
                .Where(c => c.IsActive)
                .Select(c => new ComboDto
                {
                    ComboId = c.ComboId,
                    Name = c.Name,
                    Description = c.Description ?? "",
                    ComboPrice = c.ComboPrice
                })
                .ToListAsync();
        }

        public async Task<List<ComboAdminDto>> GetAdminCombosAsync()
        {
            return await _context.Combos
                .Include(c => c.ComboProducts)
                    .ThenInclude(cp => cp.Product)
                .Where(c => c.IsActive)
                .Select(c => new ComboAdminDto
                {
                    ComboId = c.ComboId,
                    Name = c.Name,
                    ComboPrice = c.ComboPrice,

                    ImageUrl = string.IsNullOrWhiteSpace(c.ImageUrl)
                        ? "/images/combo-default.png"
                        : c.ImageUrl,

                    ProductsSummary = string.Join(", ",
                        c.ComboProducts.Select(cp =>
                            cp.Product.Name + " x" + cp.Quantity))
                })
                .ToListAsync();
        }

        // Evita que se cree/edite un combo pidiendo más unidades de un
        // producto de las que realmente hay en stock — si eso pasa, el
        // combo queda "roto" (nadie lo puede comprar nunca).
        private async Task ValidarStockParaComboAsync(List<ComboProductTemp> productos)
        {
            foreach (var prod in productos)
            {
                if (prod.Quantity <= 0) continue;

                var productStore = await _context.ProductStores
                    .Include(ps => ps.Product)
                    .FirstOrDefaultAsync(ps => ps.ProductId == prod.ProductId && ps.StoreId == Components.Shared.AppStore.StoreId);

                var nombre = productStore?.Product?.Name ?? $"Producto #{prod.ProductId}";

                if (productStore == null)
                    throw new Exception($"'{nombre}' no está disponible en esta tienda");

                if (prod.Quantity > productStore.Stock)
                    throw new Exception(
                        $"No puedes pedir {prod.Quantity} de '{nombre}' en este combo — solo hay {productStore.Stock} en stock.");
            }
        }

        public async Task<bool> CreateComboAsync(ComboCreateModel model, string? userId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (model.Products == null || !model.Products.Any())
                    throw new Exception("El combo debe tener al menos un producto");

                await ValidarStockParaComboAsync(model.Products);

                var combo = new Combo
                {
                    Name = model.Name,
                    Description = model.Description,
                    ComboPrice = model.ComboPrice,
                    ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl)
                        ? null
                        : model.ImageUrl,
                    IsActive = true
                };

                _context.Combos.Add(combo);
                await _context.SaveChangesAsync();

                foreach (var prod in model.Products)
                {
                    if (prod.Quantity <= 0)
                        continue;

                    _context.ComboProducts.Add(new ComboProduct
                    {
                        ComboId = combo.ComboId,
                        ProductId = prod.ProductId,
                        Quantity = prod.Quantity
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _audit.LogAsync(userId, "Creó combo", "Combo", combo.ComboId.ToString());

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Error creando combo: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        // Sobrecarga sin userId para compatibilidad
        public async Task<bool> CreateComboAsync(ComboCreateModel model)
            => await CreateComboAsync(model, null);

        public async Task UpdateComboAsync(int comboId, ComboCreateModel model, string? userId = null)
        {
            if (model.Products == null || !model.Products.Any())
                throw new Exception("El combo debe tener al menos un producto");

            await ValidarStockParaComboAsync(model.Products);

            var combo = await _context.Combos
                .Include(c => c.ComboProducts)
                .FirstOrDefaultAsync(c => c.ComboId == comboId);

            if (combo == null)
                throw new Exception("Combo no encontrado");

            combo.Name = model.Name;
            combo.Description = model.Description;
            combo.ComboPrice = model.ComboPrice;

            // borrar productos actuales
            _context.ComboProducts.RemoveRange(combo.ComboProducts);

            // agregar nuevos
            combo.ComboProducts = model.Products.Select(p => new ComboProduct
            {
                ProductId = p.ProductId,
                Quantity = p.Quantity
            }).ToList();

            if (!string.IsNullOrEmpty(model.ImageUrl))
                combo.ImageUrl = model.ImageUrl;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(userId, "Editó combo", "Combo", comboId.ToString());
        }

        // Sobrecarga sin userId para compatibilidad
        public async Task UpdateComboAsync(int comboId, ComboCreateModel model)
            => await UpdateComboAsync(comboId, model, null);

        public async Task DeleteComboAsync(int comboId, string? userId = null)
        {
            var combo = await _context.Combos
                .FirstOrDefaultAsync(c => c.ComboId == comboId);

            if (combo == null)
                throw new Exception("Combo no encontrado");

            combo.IsActive = false;

            await _context.SaveChangesAsync();

            await _audit.LogAsync(userId, "Eliminó combo", "Combo", comboId.ToString());
        }

        // Sobrecarga sin userId para compatibilidad
        public async Task DeleteComboAsync(int comboId)
            => await DeleteComboAsync(comboId, null);
    }
}
