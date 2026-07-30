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
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Intento de acceso al carrito con UserId nulo o vacío.");
            return new CartViewDto();
        }

        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo)
                .Include(c => c.CartPromotions).ThenInclude(cp => cp.Promotion)
                .Include(c => c.Store)
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
            _logger.LogError(ex, "Error crítico recuperando el carrito para {UserId}", userId);
            throw;
        }
    }

    public async Task<CartViewDto> AddProductToCartAsync(string userId, int productId, int quantity, int storeId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        try
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new InvalidOperationException("Producto no encontrado");

            var productStore = await _context.ProductStores
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.StoreId == storeId);

            if (productStore == null)
                throw new InvalidOperationException("Stock insuficiente");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo).ThenInclude(combo => combo.ComboProducts)
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

            // Cuánto de este producto ya está comprometido en el carrito
            // (como item directo + dentro de combos ya agregados).
            var yaReservado = GetReservedQuantity(cart, productId);
            if (yaReservado + quantity > productStore.Stock)
                throw new InvalidOperationException(
                    $"No contamos con esa cantidad de '{product.Name}'. Disponible: {productStore.Stock}, " +
                    $"ya tienes {yaReservado} en tu carrito.");

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
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        try
        {
            var combo = await _context.Combos
                .Include(c => c.ComboProducts).ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.ComboId == comboId);
            if (combo == null || !combo.IsActive)
                throw new InvalidOperationException("Combo no encontrado o inactivo");

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo).ThenInclude(cb => cb.ComboProducts)
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

            // Validar que cada producto que compone el combo tenga stock
            // suficiente para cubrir lo que ya está reservado en el carrito
            // (directo o en otros combos) más lo que este combo necesita.
            foreach (var comboProduct in combo.ComboProducts)
            {
                var productStore = await _context.ProductStores
                    .FirstOrDefaultAsync(ps => ps.ProductId == comboProduct.ProductId && ps.StoreId == storeId);

                if (productStore == null)
                    throw new InvalidOperationException(
                        $"'{comboProduct.Product?.Name ?? comboProduct.ProductId.ToString()}' no está disponible en esta tienda");

                var yaReservado = GetReservedQuantity(cart, comboProduct.ProductId);
                var necesario = quantity * comboProduct.Quantity;

                if (yaReservado + necesario > productStore.Stock)
                    throw new InvalidOperationException(
                        $"No contamos con esa cantidad de '{comboProduct.Product?.Name ?? comboProduct.ProductId.ToString()}' " +
                        $"para armar este combo. Disponible: {productStore.Stock}, ya tienes {yaReservado} en tu carrito.");
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
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        try
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionProducts)
                    .ThenInclude(pp => pp.Product)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId && p.IsActive);

            if (promotion == null)
                throw new InvalidOperationException("Promoción no encontrada o inactiva");

            if (promotion.EndDate < DateTime.UtcNow)
                throw new InvalidOperationException($"La promoción «{promotion.Title}» ya expiró");

            var cart = await _context.Carts
                .Include(c => c.CartPromotions)
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

            // Calcular el precio representativo de la promoción
            // Se usa el primer PromotionProduct para obtener el precio especial o el precio base
            var firstPromoProduct = promotion.PromotionProducts.FirstOrDefault();
            decimal unitPrice = 0;

            if (firstPromoProduct != null)
            {
                if (firstPromoProduct.SpecialPrice.HasValue)
                    unitPrice = firstPromoProduct.SpecialPrice.Value;
                else if (firstPromoProduct.DiscountPercentage.HasValue && firstPromoProduct.Product != null)
                {
                    var basePrice = await _context.ProductStores
                        .Where(ps => ps.ProductId == firstPromoProduct.ProductId && ps.StoreId == storeId)
                        .Select(ps => ps.Price)
                        .FirstOrDefaultAsync();
                    unitPrice = basePrice * (1 - firstPromoProduct.DiscountPercentage.Value / 100m);
                }
                else if (firstPromoProduct.Product != null)
                {
                    unitPrice = await _context.ProductStores
                        .Where(ps => ps.ProductId == firstPromoProduct.ProductId && ps.StoreId == storeId)
                        .Select(ps => ps.Price)
                        .FirstOrDefaultAsync();
                }
            }

            var existingItem = cart.CartPromotions.FirstOrDefault(cp => cp.PromotionId == promotionId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                existingItem.SubTotal = existingItem.Quantity * existingItem.UnitPrice;
            }
            else
            {
                var cartPromotion = new CartPromotion
                {
                    CartId = cart.CartId,
                    PromotionId = promotionId,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    SubTotal = quantity * unitPrice
                };
                cart.CartPromotions.Add(cartPromotion);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await GetCartAsync(userId, storeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding promotion to cart");
            throw;
        }
    }

    public async Task<CartViewDto> RemoveItemAsync(string userId, int cartItemId, string itemType)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        try
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos)
                .Include(c => c.CartPromotions)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            if (itemType.ToLower() == "product")
            {
                var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
                if (item != null)
                    _context.CartItems.Remove(item);
            }
            else if (itemType.ToLower() == "combo")
            {
                var item = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId);
                if (item != null)
                    _context.CartCombos.Remove(item);
            }
            else if (itemType.ToLower() == "promotion")
            {
                var item = cart.CartPromotions.FirstOrDefault(cp => cp.CartPromotionId == cartItemId);
                if (item != null)
                    _context.CartPromotions.Remove(item);
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
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        try
        {
            if (newQuantity <= 0)
                return await RemoveItemAsync(userId, cartItemId, itemType);

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo).ThenInclude(cb => cb.ComboProducts).ThenInclude(cp => cp.Product)
                .Include(c => c.CartPromotions)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            if (itemType.ToLower() == "product")
            {
                var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
                if (item != null)
                {
                    var productStore = await _context.ProductStores
                        .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.StoreId == cart.StoreId);

                    var yaReservado = GetReservedQuantity(cart, item.ProductId, excludeCartItemId: cartItemId);
                    if (productStore == null || yaReservado + newQuantity > productStore.Stock)
                        throw new InvalidOperationException(
                            $"No contamos con esa cantidad en stock. Disponible: {productStore?.Stock ?? 0}.");

                    item.Quantity = newQuantity;
                    item.SubTotal = newQuantity * item.UnitPrice;
                }
            }
            else if (itemType.ToLower() == "combo")
            {
                var item = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId);
                if (item != null)
                {
                    if (item.Combo?.ComboProducts != null)
                    {
                        foreach (var comboProduct in item.Combo.ComboProducts)
                        {
                            var productStore = await _context.ProductStores
                                .FirstOrDefaultAsync(ps => ps.ProductId == comboProduct.ProductId && ps.StoreId == cart.StoreId);

                            var yaReservado = GetReservedQuantity(cart, comboProduct.ProductId, excludeCartComboId: cartItemId);
                            var necesario = newQuantity * comboProduct.Quantity;

                            if (productStore == null || yaReservado + necesario > productStore.Stock)
                                throw new InvalidOperationException(
                                    $"No contamos con esa cantidad de '{comboProduct.Product?.Name ?? comboProduct.ProductId.ToString()}' " +
                                    $"para este combo. Disponible: {productStore?.Stock ?? 0}, ya tienes {yaReservado} en tu carrito.");
                        }
                    }

                    item.Quantity = newQuantity;
                    item.SubTotal = newQuantity * item.UnitPrice;
                }
            }
            else if (itemType.ToLower() == "promotion")
            {
                var item = cart.CartPromotions.FirstOrDefault(cp => cp.CartPromotionId == cartItemId);
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
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        return await GetCartAsync(userId, 1);
    }

    public async Task<CartViewDto> SelectAllAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        var cart = await GetCartAsync(userId, 1);
        cart.Products.ForEach(p => p.IsSelected = true);
        cart.Combos.ForEach(c => c.IsSelected = true);
        cart.Promotions.ForEach(p => p.IsSelected = true);
        return cart;
    }

    public async Task<CartViewDto> DeselectAllAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        var cart = await GetCartAsync(userId, 1);
        cart.Products.ForEach(p => p.IsSelected = false);
        cart.Combos.ForEach(c => c.IsSelected = false);
        cart.Promotions.ForEach(p => p.IsSelected = false);
        return cart;
    }

    public async Task<CartViewDto> ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

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
                _context.CartPromotions.RemoveRange(cart.CartPromotions);
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
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Intento de generar recibo con UserId nulo o vacío.");
            return null!;
        }

        try
        {
            var userEmail = await _context.Users
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            string customerDisplayName = userEmail ?? "Cliente Registrado";

            DateTimeOffset utcNow = DateTimeOffset.UtcNow;
            DateTimeOffset drOffset = utcNow.ToOffset(TimeSpan.FromHours(-4));
            DateTime localDateTime = drOffset.DateTime;

            var cart = await _context.Carts
                .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
                .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo)
                .Include(c => c.Store)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado");

            // Usamos MapToCartViewDto que ahora devuelve UNA card por item con Quantity real
            var cartView = MapToCartViewDto(cart);

            var receipt = new ReceiptDto
            {
                ReceiptNumber = $"REC-{localDateTime:yyyyMMddHHmmss}-{userId.Substring(0, 4)}",
                CreatedAt = localDateTime,
                CustomerName = customerDisplayName,
                StoreLocation = cart.Store.Name
            };

            // selectedOnly se maneja en el frontend con IsSelected;
            // el servicio genera el recibo COMPLETO y el frontend filtra
            foreach (var product in cartView.Products)
            {
                receipt.Items.Add(new ReceiptItemDto
                {
                    ItemId = product.CartItemId,
                    ItemType = "product",
                    ItemName = product.ProductName,
                    Category = "Producto",
                    Quantity = product.Quantity,
                    UnitPrice = product.UnitPrice,
                    SubTotal = product.SubTotal
                });
            }

            foreach (var combo in cartView.Combos)
            {
                receipt.Items.Add(new ReceiptItemDto
                {
                    ItemId = combo.CartComboId,
                    ItemType = "combo",
                    ItemName = combo.ComboName,
                    Category = "Combo",
                    Quantity = combo.Quantity,
                    UnitPrice = combo.UnitPrice,
                    SubTotal = combo.SubTotal
                });
            }

            foreach (var promo in cartView.Promotions)
            {
                receipt.Items.Add(new ReceiptItemDto
                {
                    ItemId = promo.CartPromotionId,
                    ItemType = "promotion",
                    ItemName = promo.PromotionName,
                    Category = promo.Category,
                    Quantity = promo.Quantity,
                    UnitPrice = promo.UnitPrice,
                    SubTotal = promo.SubTotal
                });
            }

            receipt.TotalAmount = cartView.TotalPrice;

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
        if (string.IsNullOrWhiteSpace(userId))
            throw new Exception("UserId inválido en CartService");

        // Si dos personas compran el mismo producto casi al mismo tiempo,
        // la segunda en llegar a SaveChangesAsync recibe un
        // DbUpdateConcurrencyException (el RowVersion de ProductStore ya
        // cambió). En vez de fallar directo, reintentamos con el stock
        // recién leído — igual que reaccionaría un cajero si otro cliente
        // se le adelantó: revisa de nuevo antes de decir que no hay.
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            // Limpiamos el tracker al reintentar: las entidades del intento
            // anterior tienen datos de stock que ya no son válidos.
            if (attempt > 1)
                _context.ChangeTracker.Clear();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .Include(c => c.CartCombos)
                        .ThenInclude(cc => cc.Combo)
                            .ThenInclude(combo => combo.ComboProducts)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.CartStatus == "Active");

                if (cart == null)
                    return false;

                // ── 1. DESCONTAR STOCK DE PRODUCTOS DIRECTOS ─────────────────────────
                foreach (var cartItem in cart.CartItems)
                {
                    var productStore = await _context.ProductStores
                        .FirstOrDefaultAsync(ps => ps.ProductId == cartItem.ProductId
                                                && ps.StoreId == cart.StoreId);

                    if (productStore == null)
                    {
                        _logger.LogWarning(
                            "ProductStore no encontrado para ProductId={ProductId} StoreId={StoreId}. Se omite.",
                            cartItem.ProductId, cart.StoreId);
                        continue;
                    }

                    // Validar stock suficiente (protección extra en checkout)
                    if (productStore.Stock < cartItem.Quantity)
                        throw new InvalidOperationException(
                            $"Stock insuficiente para '{cartItem.Product?.Name ?? cartItem.ProductId.ToString()}'. " +
                            $"Disponible: {productStore.Stock}, solicitado: {cartItem.Quantity}.");

                    productStore.Stock -= cartItem.Quantity;
                    productStore.IsAvailable = productStore.Stock > 0;

                    // Auditoría de movimiento
                    _context.StockMovements.Add(new StockMovement
                    {
                        ProductId = cartItem.ProductId,
                        StoreId = cart.StoreId,
                        Quantity = cartItem.Quantity,
                        MovementType = "Out",
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // ── 2. DESCONTAR STOCK DE COMBOS (cada producto dentro del combo) ────
                foreach (var cartCombo in cart.CartCombos)
                {
                    if (cartCombo.Combo?.ComboProducts == null) continue;

                    foreach (var comboProduct in cartCombo.Combo.ComboProducts)
                    {
                        var productStore = await _context.ProductStores
                            .FirstOrDefaultAsync(ps => ps.ProductId == comboProduct.ProductId
                                                    && ps.StoreId == cart.StoreId);

                        if (productStore == null)
                        {
                            _logger.LogWarning(
                                "ProductStore no encontrado para combo ProductId={ProductId}. Se omite.",
                                comboProduct.ProductId);
                            continue;
                        }

                        // La cantidad total a descontar = unidades del combo × cantidad en el combo
                        var totalQty = cartCombo.Quantity * comboProduct.Quantity;

                        if (productStore.Stock < totalQty)
                            throw new InvalidOperationException(
                                $"Stock insuficiente para producto en combo (ProductId={comboProduct.ProductId}). " +
                                $"Disponible: {productStore.Stock}, requerido: {totalQty}.");

                        productStore.Stock -= totalQty;
                        productStore.IsAvailable = productStore.Stock > 0;

                        _context.StockMovements.Add(new StockMovement
                        {
                            ProductId = comboProduct.ProductId,
                            StoreId = cart.StoreId,
                            Quantity = totalQty,
                            MovementType = "Out",
                            UserId = userId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // ── 3. CREAR LA ORDEN EN LA TABLA Orders ─────────────────────────────
                // Calcular el total sumando productos + combos + promociones del carrito
                decimal orderTotal = cart.CartItems.Sum(ci => ci.SubTotal)
                                   + cart.CartCombos.Sum(cc => cc.SubTotal)
                                   + cart.CartPromotions.Sum(cp => cp.SubTotal);

                var order = new Order
                {
                    UserId = userId,
                    StoreId = cart.StoreId,
                    OrderStatus = "Completed",
                    PaymentMethod = "WhatsApp",
                    TotalAmount = orderTotal,
                    CreatedAt = DateTime.Now
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // necesitamos OrderId generado

                // Crear OrderItems solo para productos directos (los combos no tienen
                // una tabla OrderItem por diseño; se registran vía StockMovement)
                foreach (var ci in cart.CartItems)
                {
                    _context.OrderItems.Add(new OrderItem
                    {
                        OrderId = order.OrderId,
                        ProductId = ci.ProductId,
                        Quantity = ci.Quantity,
                        UnitPrice = ci.UnitPrice
                    });
                }

                // ── 4. CERRAR EL CARRITO ─────────────────────────────────────────────
                cart.CartStatus = "Sent";
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Checkout completado para UserId={UserId}. CartId={CartId}.",
                    userId, cart.CartId);

                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex,
                    "Conflicto de concurrencia en checkout (intento {Attempt}/{MaxRetries}) para UserId={UserId} — otra venta modificó el mismo stock al mismo tiempo.",
                    attempt, maxRetries, userId);

                if (attempt == maxRetries)
                    throw new InvalidOperationException(
                        "Uno de los productos de tu carrito se acaba de vender en otra compra. Revisa tu carrito e intenta de nuevo.");

                // si no fue el último intento, el for vuelve a empezar
                // con el stock recién leído de la base de datos
            }
            catch (InvalidOperationException ex)
            {
                // Stock insuficiente — rollback y re-lanzar para que el frontend muestre el mensaje
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Checkout cancelado por stock insuficiente para UserId={UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during checkout para UserId={UserId}", userId);
                return false;
            }
        }

        return false; // no debería llegarse aquí
    }

    // Suma cuánta unidad de un producto ya está comprometida en el carrito:
    // como item directo (CartItems) y como parte de combos ya agregados
    // (CartCombos → Combo.ComboProducts). Es la base para no dejar pasar
    // más de lo que hay en stock, sin importar por dónde se intente colar
    // (uno por uno, combo, o el +/- del carrito).
    private static int GetReservedQuantity(Cart cart, int productId, int? excludeCartItemId = null, int? excludeCartComboId = null)
    {
        var reservado = cart.CartItems
            .Where(ci => ci.ProductId == productId && ci.CartItemId != excludeCartItemId)
            .Sum(ci => ci.Quantity);

        foreach (var cc in cart.CartCombos.Where(cc => cc.CartComboId != excludeCartComboId))
        {
            if (cc.Combo?.ComboProducts == null) continue;

            foreach (var cp in cc.Combo.ComboProducts.Where(cp => cp.ProductId == productId))
            {
                reservado += cc.Quantity * cp.Quantity;
            }
        }

        return reservado;
    }

    // FIX PRINCIPAL: UNA card por item con Quantity real — sin expandir duplicados
    private CartViewDto MapToCartViewDto(Cart cart)
    {
        var result = new CartViewDto { CartId = cart.CartId };

        foreach (var item in cart.CartItems)
        {
            result.Products.Add(new CartItemDisplayDto
            {
                CartItemId = item.CartItemId,
                ProductId = item.ProductId,
                ProductName = item.Product.Name,
                ProductImage = item.Product.ImageUrl,
                Category = "Producto",
                Quantity = item.Quantity,          // ← cantidad real, no siempre 1
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,          // ← subtotal real (precio × cantidad)
                IsSelected = false
            });
        }

        foreach (var item in cart.CartCombos)
        {
            result.Combos.Add(new CartComboDisplayDto
            {
                CartComboId = item.CartComboId,
                ComboId = item.ComboId,
                ComboName = item.Combo.Name,
                ComboImage = item.Combo.ImageUrl,
                Category = "Combo",
                Quantity = item.Quantity,           // ← cantidad real
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,           // ← subtotal real
                IsSelected = false
            });
        }

        foreach (var item in cart.CartPromotions)
        {
            result.Promotions.Add(new CartPromotionDisplayDto
            {
                CartPromotionId = item.CartPromotionId,
                PromotionId = item.PromotionId,
                PromotionName = item.Promotion.Title,
                PromotionImage = item.Promotion.ImageUrl,
                Category = "Promoción",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                IsSelected = false
            });
        }

        return result;
    }
}
