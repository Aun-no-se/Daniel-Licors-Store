using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class ComboService : IComboService
    {
        private readonly ApplicationDbContext _context;

        public ComboService(ApplicationDbContext context)
        {
            _context = context;
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
            if (!await ValidateComboStockAsync(comboId, storeId, quantity))
                return false;

            var comboProducts = await _context.ComboProducts
                .Where(cp => cp.ComboId == comboId)
                .ToListAsync();

            foreach (var item in comboProducts)
            {
                var productStore = await _context.ProductStores
                    .FirstAsync(ps =>
                        ps.ProductId == item.ProductId &&
                        ps.StoreId == storeId);

                var required = item.Quantity * quantity;

                productStore.Stock -= required;

                _context.StockMovements.Add(new StockMovement
                {
                    ProductId = item.ProductId,
                    StoreId = storeId,
                    Quantity = required,
                    MovementType = "Out",
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ComboDto>> GetCombosByProductAsync(int productId)
        {
            return await _context.ComboProducts
                .Where(cp => cp.ProductId == productId && cp.Combo.IsActive)
                .Select(cp => new ComboDto
                {
                    ComboId = cp.ComboId,
                    Name = cp.Combo.Name,
                    Description = cp.Combo.Description!,
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
                    Description = c.Description,
                    ComboPrice = c.ComboPrice,
                    Products = c.ComboProducts.Select(cp => new ComboProductDto
                    {
                        ProductId = cp.ProductId,
                        ProductName = cp.Product.Name,
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
                    Description = c.Description!,
                    ComboPrice = c.ComboPrice
                })
                .ToListAsync();
        }

        public async Task<List<ComboAdminDto>> GetAdminCombosAsync()
        {
            return await _context.Combos
                .Where(c => c.IsActive)
                .Select(c => new ComboAdminDto
                {
                    ComboId = c.ComboId,
                    Name = c.Name,
                    ComboPrice = c.ComboPrice,
                    ImageUrl = "/images/combo-default.png",

                    ProductsSummary = string.Join(", ",
                        c.ComboProducts.Select(cp =>
                            cp.Product.Name + " x" + cp.Quantity))
                })
                .ToListAsync();
        }
    }
}
