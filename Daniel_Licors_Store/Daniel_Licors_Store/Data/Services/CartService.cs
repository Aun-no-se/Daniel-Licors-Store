using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Cart;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services;

public interface ICartService
{
    Task<CartViewDto> GetCartAsync(string userId, int storeId);
    Task<CartViewDto> AddProductToCartAsync(string userId, int productId, int quantity, int storeId);
    Task<CartViewDto> AddComboToCartAsync(string userId, int comboId, int quantity, int storeId);
    Task<CartViewDto> AddPromotionToCartAsync(string userId, int promotionId, int quantity, int storeId);
    Task<CartViewDto> RemoveItemAsync(string userId, int cartItemId, string itemType);
    Task<CartViewDto> UpdateItemQuantityAsync(string userId, int cartItemId, int newQuantity, string itemType);
    Task<CartViewDto> UpdateItemSelectionAsync(string userId, int cartItemId, bool isSelected, string itemType);
    Task<CartViewDto> SelectAllAsync(string userId);
    Task<CartViewDto> DeselectAllAsync(string userId);
    Task<CartViewDto> ClearCartAsync(string userId);
    Task<ReceiptDto> GenerateReceiptAsync(string userId, bool selectedOnly = false);
    Task<bool> CheckoutAsync(string userId, List<int> selectedItemIds, string itemType);
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CartService> _logger;

    public CartService(ApplicationDbContext context, ILogger<CartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CartViewDto> GetCartAsync(string userId, int storeId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.StoreId == storeId && c.CartStatus == "Active");

            if (cart == null)
            {
                cart = new Cart 
                { 
                    UserId = userId, 
                    StoreId = storeId, 
                    CartStatus = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return MapToCartViewDto(cart);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cart for user {UserId}", userId);
            throw;
        }
    }

    public async Task<CartViewDto> AddProductToCartAsync(string userId, int productId, int quantity, int storeId)
    {
        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Producto no encontrado");

            var productStore = await _context.ProductStores
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.StoreId == storeId);

            if (productStore == null || productStore.Stock < quantity)
                throw new InvalidOperationException("Stock insuficiente");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.StoreId == storeId && c.CartStatus == "Active");

            if (cart == null)
            {
                cart = new Cart 
                { 
                    UserId = userId, 
                    StoreId = storeId, 
                    CartStatus = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.SubTotal = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = productStore.Price,
                    SubTotal = quantity * productStore.Price
                };
                cart.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCartAsync(userId, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding product to cart");
            throw;
        }
    }

    public async Task<CartViewDto> AddComboToCartAsync(string userId, int comboId, int quantity, int storeId)
    {
        try
        {
            var combo = await _context.Combos.FindAsync(comboId);
            if (combo == null || !combo.IsActive)
                throw new InvalidOperationException("Combo no encontrado o inactivo");

            var cart = await _context.Carts
                .Include(c => c.CartCombos)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.StoreId == storeId && c.CartStatus == "Active");

            if (cart == null)
            {
                cart = new Cart 
                { 
                    UserId = userId, 
                    StoreId = storeId, 
                    CartStatus = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingItem = cart.CartCombos.FirstOrDefault(cc => cc.ComboId == comboId);
            
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.SubTotal = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                var cartCombo = new CartCombo
                {
                    CartId = cart.CartId,
                    ComboId = comboId,
                    Quantity = quantity,
                    UnitPrice = combo.ComboPrice,
                    SubTotal = quantity * combo.ComboPrice
                };
                cart.CartCombos.Add(cartCombo);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCartAsync(userId, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding combo to cart");
            throw;
        }
    }

    public async Task<CartViewDto> AddPromotionToCartAsync(string userId, int promotionId, int quantity, int storeId)
    {
        // Nota: Las promociones se manejan como items especiales
        // Por ahora, retornamos una promoción como un combo especial
        // Esto puede ser expandido si tienes una tabla CartPromotion
        throw new NotImplementedException("Funcionalidad de promociones en carrito será implementada");
    }

    public async Task<CartViewDto> RemoveItemAsync(string userId, int cartItemId, string itemType)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            if (itemType.ToLower() == "product")
            {
                var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                }
            }
            else if (itemType.ToLower() == "combo")
            {
                var item = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId);
                if (item != null)
                {
                    _context.CartCombos.Remove(item);
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCartAsync(userId, cart.StoreId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing item from cart");
            throw;
        }
    }

    public async Task<CartViewDto> UpdateItemQuantityAsync(string userId, int cartItemId, int newQuantity, string itemType)
    {
        try
        {
            if (newQuantity <= 0)
                return await RemoveItemAsync(userId, cartItemId, itemType);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            if (itemType.ToLower() == "product")
            {
                var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
                if (item != null)
                {
                    item.Quantity = newQuantity;
                    item.SubTotal = newQuantity * item.UnitPrice;
                }
            }
            else if (itemType.ToLower() == "combo")
            {
                var item = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId);
                if (item != null)
                {
                    item.Quantity = newQuantity;
                    item.SubTotal = newQuantity * item.UnitPrice;
                }
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCartAsync(userId, cart.StoreId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item quantity");
            throw;
        }
    }

    public async Task<CartViewDto> UpdateItemSelectionAsync(string userId, int cartItemId, bool isSelected, string itemType)
    {
        // Esta funcionalidad se maneja en el frontend con binding de datos
        // El servicio mantiene el estado en memoria durante la sesión
        // Para persistencia, necesitarías agregar un campo IsSelected a las entidades
        return await GetCartAsync(userId, 1); // StoreId por defecto
    }

    public async Task<CartViewDto> SelectAllAsync(string userId)
    {
        var cart = await GetCartAsync(userId, 1);
        cart.Products.ForEach(p => p.IsSelected = true);
        cart.Combos.ForEach(c => c.IsSelected = true);
        cart.Promotions.ForEach(p => p.IsSelected = true);
        return cart;
    }

    public async Task<CartViewDto> DeselectAllAsync(string userId)
    {
        var cart = await GetCartAsync(userId, 1);
        cart.Products.ForEach(p => p.IsSelected = false);
        cart.Combos.ForEach(c => c.IsSelected = false);
        cart.Promotions.ForEach(p => p.IsSelected = false);
        return cart;
    }

    public async Task<CartViewDto> ClearCartAsync(string userId)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                _context.CartCombos.RemoveRange(cart.CartCombos);
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return await GetCartAsync(userId, 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            throw;
        }
    }

    public async Task<ReceiptDto> GenerateReceiptAsync(string userId, bool selectedOnly = false)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo)
                .Include(c => c.User)
                .Include(c => c.Store)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            var cartView = MapToCartViewDto(cart);
            
            var receipt = new ReceiptDto
            {
                ReceiptNumber = $"REC-{DateTime.UtcNow:yyyyMMddHHmmss}-{userId.Substring(0, 4)}",
                CreatedAt = DateTime.UtcNow,
                CustomerName = cart.User?.FullName ?? "Cliente",
                StoreLocation = cart.Store.Name
            };

            // Agregar items del carrito
            foreach (var product in cartView.Products.Where(p => !selectedOnly || p.IsSelected))
            {
                receipt.Items.Add(new ReceiptItemDto
                {
                    ItemName = product.ProductName,
                    Category = "Producto",
                    Quantity = product.Quantity,
                    UnitPrice = product.UnitPrice,
                    SubTotal = product.SubTotal
                });
            }

            foreach (var combo in cartView.Combos.Where(c => !selectedOnly || c.IsSelected))
            {
                receipt.Items.Add(new ReceiptItemDto
                {
                    ItemName = combo.ComboName,
                    Category = "Combo",
                    Quantity = combo.Quantity,
                    UnitPrice = combo.UnitPrice,
                    SubTotal = combo.SubTotal
                });
            }

            receipt.TotalAmount = selectedOnly ? cartView.SelectedPrice : cartView.TotalPrice;

            return receipt;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating receipt");
            throw;
        }
    }

    public async Task<bool> CheckoutAsync(string userId, List<int> selectedItemIds, string itemType)
    {
        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                return false;

            // Aquí iría la lógica de crear una orden
            // Por ahora, solo registramos que se completó el checkout
            
            cart.CartStatus = "Sent";
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during checkout");
            return false;
        }
    }

    private CartViewDto MapToCartViewDto(Cart cart)
    {
        var result = new CartViewDto { CartId = cart.CartId };

        // Mapear productos
        foreach (var item in cart.CartItems)
        {
            result.Products.Add(new CartItemDisplayDto
            {
                CartItemId = item.CartItemId,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductImage = item.Product.ImageUrl,
                Category = "Producto",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                IsSelected = false
            });
        }

        // Mapear combos
        foreach (var item in cart.CartCombos)
        {
            result.Combos.Add(new CartComboDisplayDto
            {
                CartComboId = item.CartComboId,
                ComboId = item.ComboId,
                ComboName = item.Combo.Name,
                ComboImage = item.Combo.ImageUrl,
                Category = "Combo",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                IsSelected = false
            });
        }

        return result;
    }
}
