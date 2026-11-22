using System.Data;
using System.Security.Cryptography;
using System.Text;
using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Cart;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services;

public interface ICartService
{
    /// <summary>Lectura pura del carrito (no modifica nada).</summary>
    Task<CartViewDto> GetCartAsync(string userId, int storeId);

    /// <summary>
    /// Pone el carrito al día con la realidad: quita lo que se agotó o se ocultó, baja cantidades
    /// al stock real y refresca precios. Lo que cambió viene en CartViewDto.Adjustments.
    /// </summary>
    Task<CartViewDto> ReconcileCartAsync(string userId, int storeId);

    Task<CartViewDto> AddProductToCartAsync(string userId, int productId, int quantity, int storeId);
    Task<CartViewDto> AddComboToCartAsync(string userId, int comboId, int quantity, int storeId);
    Task<CartViewDto> AddPromotionToCartAsync(string userId, int promotionId, int quantity, int storeId);
    Task<CartViewDto> RemoveItemAsync(string userId, int cartItemId, string itemType);
    Task<CartViewDto> UpdateItemQuantityAsync(string userId, int cartItemId, int newQuantity, string itemType);
    Task<CartViewDto> UpdateItemSelectionAsync(string userId, int cartItemId, bool isSelected, string itemType);
    Task<CartViewDto> SelectAllAsync(string userId);
    Task<CartViewDto> DeselectAllAsync(string userId);
    Task<CartViewDto> ClearCartAsync(string userId);

    /// <summary>Recibo de lo que se va a comprar. selection = null significa todo el carrito.</summary>
    Task<ReceiptDto> GenerateReceiptAsync(string userId, CheckoutSelectionDto? selection = null);

    /// <summary>
    /// Compra definitiva. Todo o nada: o se descuenta el stock de TODO lo comprado y se crea
    /// el pedido, o no se toca nada. selection = null significa todo el carrito.
    /// Devuelve false si no había nada que comprar. Lanza InvalidOperationException con un
    /// mensaje listo para mostrar al cliente si algo no se puede comprar.
    /// </summary>
    Task<bool> CheckoutAsync(string userId, CheckoutSelectionDto? selection = null);
}

public class CartService : ICartService
{
    // ═══════════════════════════════════════════════════════════════════════
    // REGLAS DE STOCK DEL CARRITO — resumen
    //
    // 1. El carrito NO reserva stock (un carrito olvidado una semana no puede
    //    bloquear bebidas). Lo que se valida al agregar es una comprobación
    //    de comodidad; la garantía real está en CheckoutAsync.
    //
    // 2. Al agregar o cambiar cantidades se suma TODO lo que el carrito
    //    necesita de cada bebida (producto suelto + combos + promociones) y
    //    se compara contra el stock real leído de la base en ese momento.
    //
    // 3. CheckoutAsync descuenta el stock con un UPDATE condicional atómico:
    //        UPDATE ... SET Stock = Stock - @q WHERE Stock >= @q
    //    SQL Server serializa ese UPDATE por fila, así que dos clientes
    //    comprando a la vez NUNCA pueden dejar el stock en negativo: el
    //    segundo recibe 0 filas afectadas y la compra completa se revierte.
    //
    // 4. Todo el checkout corre dentro de UNA transacción. Si la app se cierra,
    //    se cae la conexión o algo falla a la mitad, SQL Server revierte todo:
    //    no se descuenta stock ni se crea pedido a medias.
    //
    // 5. Cada operación usa su propio DbContext (IDbContextFactory) para no
    //    leer stock viejo guardado en el change tracker del circuito de Blazor,
    //    y un candado por usuario (sp_getapplock) evita que dos pestañas o un
    //    doble clic del mismo cliente se pisen entre sí.
    //
    // 6. La base de datos tiene la última palabra: script 06 agrega
    //    CHECK (Stock >= 0), CHECK (Quantity > 0) e índices únicos.
    // ═══════════════════════════════════════════════════════════════════════

    private const int DefaultStoreId = 1;
    private const int MaxQuantityPerLine = 10_000;
    private const int MaxCheckoutAttempts = 3;

    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ILogger<CartService> _logger;

    public CartService(IDbContextFactory<ApplicationDbContext> factory, ILogger<CartService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ───────────────────────────────────────────────────────────────────────
    // LECTURA
    // ───────────────────────────────────────────────────────────────────────

    public async Task<CartViewDto> GetCartAsync(string userId, int storeId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Intento de acceso al carrito con UserId nulo o vacío.");
            return new CartViewDto();
        }

        return await GuardAsync("leer carrito", async () =>
        {
            var (view, _) = await BuildViewAsync(userId, storeId);
            return view;
        });
    }

    public async Task<CartViewDto> ReconcileCartAsync(string userId, int storeId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new CartViewDto();

        var adjustments = await GuardAsync("revisar carrito", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var cart = await GetOrCreateActiveCartAsync(db, userId, storeId);
            MergeDuplicateLines(db, cart);

            var list = new List<string>();
            var now = DateTime.Now;

            var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart));
            var remaining = ProductIdsOf(cart).Distinct()
                .ToDictionary(id => id, id => stock.TryGetValue(id, out var r) ? Math.Max(r.Stock, 0) : 0);

            // Orden de reparto del stock disponible: productos sueltos → combos → promociones.

            // ── Productos sueltos ──
            foreach (var ci in cart.CartItems.OrderBy(i => i.CartItemId).ToList())
            {
                var name = ci.Product?.DisplayName ?? "Un producto";

                if (ci.Product == null || !ci.Product.IsActive || !stock.ContainsKey(ci.ProductId))
                {
                    cart.CartItems.Remove(ci);
                    db.CartItems.Remove(ci);
                    list.Add($"«{name}» ya no está disponible y se quitó de tu carrito.");
                    continue;
                }

                var rem = remaining[ci.ProductId];
                var allowed = Math.Min(Math.Max(ci.Quantity, 0), rem);

                if (allowed <= 0)
                {
                    cart.CartItems.Remove(ci);
                    db.CartItems.Remove(ci);
                    list.Add($"«{name}» se agotó y se quitó de tu carrito.");
                    continue;
                }

                if (allowed < ci.Quantity)
                {
                    list.Add($"Ajustamos «{name}» a {allowed} unidad(es): es lo que tenemos disponible.");
                    ci.Quantity = allowed;
                }

                remaining[ci.ProductId] = rem - allowed;

                var price = stock[ci.ProductId].Price;
                if (ci.UnitPrice != price)
                {
                    list.Add($"El precio de «{name}» cambió a ${price:N2}.");
                    ci.UnitPrice = price;
                }
                ci.SubTotal = ci.Quantity * ci.UnitPrice;
            }

            // ── Combos ──
            foreach (var cc in cart.CartCombos.OrderBy(c => c.CartComboId).ToList())
            {
                var combo = cc.Combo;
                var name = combo?.Name ?? "Un combo";
                var perUnit = PerUnitOfCombo(cc);

                var valid = combo != null
                            && combo.IsActive
                            && InDateRange(combo.StartDate, combo.EndDate, now)
                            && perUnit.Count > 0
                            && combo.ComboProducts.All(cp => cp.Product != null
                                                             && cp.Product.IsActive
                                                             && stock.ContainsKey(cp.ProductId));
                if (!valid)
                {
                    cart.CartCombos.Remove(cc);
                    db.CartCombos.Remove(cc);
                    list.Add($"El combo «{name}» ya no está disponible y se quitó de tu carrito.");
                    continue;
                }

                var maxUnits = perUnit.Min(kv => remaining[kv.Key] / kv.Value);
                var allowed = Math.Min(Math.Max(cc.Quantity, 0), maxUnits);

                if (allowed <= 0)
                {
                    cart.CartCombos.Remove(cc);
                    db.CartCombos.Remove(cc);
                    list.Add($"El combo «{name}» se agotó y se quitó de tu carrito.");
                    continue;
                }

                if (allowed < cc.Quantity)
                {
                    list.Add($"Ajustamos el combo «{name}» a {allowed} unidad(es): es lo que podemos armar con el stock actual.");
                    cc.Quantity = allowed;
                }

                foreach (var kv in perUnit)
                    remaining[kv.Key] -= allowed * kv.Value;

                if (cc.UnitPrice != combo!.ComboPrice)
                {
                    list.Add($"El precio del combo «{name}» cambió a ${combo.ComboPrice:N2}.");
                    cc.UnitPrice = combo.ComboPrice;
                }
                cc.SubTotal = cc.Quantity * cc.UnitPrice;
            }

            // ── Promociones ──
            foreach (var cp in cart.CartPromotions.OrderBy(p => p.CartPromotionId).ToList())
            {
                var promo = cp.Promotion;
                var name = promo?.Title ?? "Una promoción";
                var perUnit = PerUnitOfPromo(cp);

                var valid = promo != null
                            && promo.IsActive
                            && promo.StartDate <= now
                            && promo.EndDate >= now
                            && perUnit.Count > 0
                            && promo.PromotionProducts.All(pp => pp.Product != null
                                                                 && pp.Product.IsActive
                                                                 && stock.ContainsKey(pp.ProductId));
                if (!valid)
                {
                    cart.CartPromotions.Remove(cp);
                    db.CartPromotions.Remove(cp);
                    list.Add($"La promoción «{name}» ya no está disponible y se quitó de tu carrito.");
                    continue;
                }

                var maxUnits = perUnit.Min(kv => remaining[kv.Key] / kv.Value);
                var allowed = Math.Min(Math.Max(cp.Quantity, 0), maxUnits);

                if (allowed <= 0)
                {
                    cart.CartPromotions.Remove(cp);
                    db.CartPromotions.Remove(cp);
                    list.Add($"La promoción «{name}» se agotó y se quitó de tu carrito.");
                    continue;
                }

                if (allowed < cp.Quantity)
                {
                    list.Add($"Ajustamos la promoción «{name}» a {allowed} unidad(es): es lo que tenemos disponible.");
                    cp.Quantity = allowed;
                }

                foreach (var kv in perUnit)
                    remaining[kv.Key] -= allowed * kv.Value;

                cp.SubTotal = cp.Quantity * cp.UnitPrice;
            }

            if (db.ChangeTracker.HasChanges())
            {
                cart.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            await tx.CommitAsync();
            return list;
        });

        var (view, _) = await BuildViewAsync(userId, storeId);
        view.Adjustments.AddRange(adjustments);
        return view;
    }

    // ───────────────────────────────────────────────────────────────────────
    // AGREGAR
    // ───────────────────────────────────────────────────────────────────────

    public async Task<CartViewDto> AddProductToCartAsync(string userId, int productId, int quantity, int storeId)
    {
        RequireUser(userId);
        RequireQuantity(quantity);

        await GuardAsync("agregar producto", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null || !product.IsActive)
                throw new InvalidOperationException("Esta bebida ya no está disponible.");

            var cart = await GetOrCreateActiveCartAsync(db, userId, storeId);
            MergeDuplicateLines(db, cart);

            var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart).Append(productId));
            if (!stock.TryGetValue(productId, out var row))
                throw new InvalidOperationException($"'{product.DisplayName}' no está disponible en esta tienda.");

            var names = NamesOf(cart);
            names[productId] = product.DisplayName;

            var existing = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UnitPrice = row.Price;
                existing.SubTotal = existing.Quantity * existing.UnitPrice;
            }
            else
            {
                cart.CartItems.Add(new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = row.Price,
                    SubTotal = quantity * row.Price
                });
            }

            // Se valida DESPUÉS de aplicar el cambio en memoria: así cuenta lo que ya
            // había en el carrito como producto suelto, dentro de combos y de promociones.
            EnsureStock(cart, new[] { productId }, stock, names);

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        });

        return await GetCartAsync(userId, storeId);
    }

    public async Task<CartViewDto> AddComboToCartAsync(string userId, int comboId, int quantity, int storeId)
    {
        RequireUser(userId);
        RequireQuantity(quantity);

        await GuardAsync("agregar combo", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var combo = await db.Combos
                .Include(c => c.ComboProducts).ThenInclude(cp => cp.Product)
                .FirstOrDefaultAsync(c => c.ComboId == comboId);

            if (combo == null || !combo.IsActive || !InDateRange(combo.StartDate, combo.EndDate, DateTime.Now))
                throw new InvalidOperationException("Este combo ya no está disponible.");

            if (combo.ComboProducts.Count == 0)
                throw new InvalidOperationException("Este combo no tiene productos.");

            foreach (var cp in combo.ComboProducts)
            {
                if (cp.Product == null || !cp.Product.IsActive)
                    throw new InvalidOperationException(
                        $"'{cp.Product?.DisplayName ?? "Un producto del combo"}' ya no está disponible, por eso no se puede armar este combo.");
            }

            var cart = await GetOrCreateActiveCartAsync(db, userId, storeId);
            MergeDuplicateLines(db, cart);

            var comboProductIds = combo.ComboProducts.Select(cp => cp.ProductId).ToList();
            var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart).Concat(comboProductIds));

            foreach (var cp in combo.ComboProducts)
            {
                if (!stock.ContainsKey(cp.ProductId))
                    throw new InvalidOperationException($"'{cp.Product.DisplayName}' no está disponible en esta tienda.");
            }

            var names = NamesOf(cart);
            foreach (var cp in combo.ComboProducts)
                names[cp.ProductId] = cp.Product.DisplayName;

            var existing = cart.CartCombos.FirstOrDefault(cc => cc.ComboId == comboId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UnitPrice = combo.ComboPrice;
                existing.SubTotal = existing.Quantity * existing.UnitPrice;
            }
            else
            {
                cart.CartCombos.Add(new CartCombo
                {
                    CartId = cart.CartId,
                    ComboId = comboId,
                    Combo = combo,
                    Quantity = quantity,
                    UnitPrice = combo.ComboPrice,
                    SubTotal = quantity * combo.ComboPrice
                });
            }

            // Un combo consume stock de cada bebida que lo compone: se suma junto con
            // los productos sueltos y las promociones que ya estén en el carrito.
            EnsureStock(cart, comboProductIds, stock, names);

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        });

        return await GetCartAsync(userId, storeId);
    }

    public async Task<CartViewDto> AddPromotionToCartAsync(string userId, int promotionId, int quantity, int storeId)
    {
        RequireUser(userId);
        RequireQuantity(quantity);

        await GuardAsync("agregar promoción", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var promotion = await db.Promotions
                .Include(p => p.PromotionProducts).ThenInclude(pp => pp.Product)
                .FirstOrDefaultAsync(p => p.PromotionId == promotionId);

            var now = DateTime.Now;
            if (promotion == null || !promotion.IsActive)
                throw new InvalidOperationException("Promoción no encontrada o inactiva.");
            if (promotion.EndDate < now)
                throw new InvalidOperationException($"La promoción «{promotion.Title}» ya expiró.");
            if (promotion.StartDate > now)
                throw new InvalidOperationException($"La promoción «{promotion.Title}» todavía no está vigente.");
            if (promotion.PromotionProducts.Count == 0)
                throw new InvalidOperationException($"La promoción «{promotion.Title}» no tiene productos.");

            foreach (var pp in promotion.PromotionProducts)
            {
                if (pp.Product == null || !pp.Product.IsActive)
                    throw new InvalidOperationException(
                        $"'{pp.Product?.DisplayName ?? "Un producto de la promoción"}' ya no está disponible.");
            }

            var cart = await GetOrCreateActiveCartAsync(db, userId, storeId);
            MergeDuplicateLines(db, cart);

            var promoProductIds = promotion.PromotionProducts.Select(pp => pp.ProductId).Distinct().ToList();
            var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart).Concat(promoProductIds));

            foreach (var pp in promotion.PromotionProducts)
            {
                if (!stock.ContainsKey(pp.ProductId))
                    throw new InvalidOperationException($"'{pp.Product.DisplayName}' no está disponible en esta tienda.");
            }

            var names = NamesOf(cart);
            foreach (var pp in promotion.PromotionProducts)
                names[pp.ProductId] = pp.Product.DisplayName;

            // Precio representativo de la promoción (misma lógica que ya tenías).
            decimal unitPrice = 0m;
            var first = promotion.PromotionProducts.OrderBy(pp => pp.PromotionProductId).FirstOrDefault();
            if (first != null)
            {
                var basePrice = stock[first.ProductId].Price;
                if (first.SpecialPrice.HasValue)
                    unitPrice = first.SpecialPrice.Value;
                else if (first.DiscountPercentage.HasValue)
                    unitPrice = basePrice * (1 - first.DiscountPercentage.Value / 100m);
                else
                    unitPrice = basePrice;
            }

            var existing = cart.CartPromotions.FirstOrDefault(cp => cp.PromotionId == promotionId);
            if (existing != null)
            {
                existing.Quantity += quantity;
                existing.UnitPrice = unitPrice;
                existing.SubTotal = existing.Quantity * existing.UnitPrice;
            }
            else
            {
                cart.CartPromotions.Add(new CartPromotion
                {
                    CartId = cart.CartId,
                    PromotionId = promotionId,
                    Promotion = promotion,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    SubTotal = quantity * unitPrice
                });
            }

            EnsureStock(cart, promoProductIds, stock, names);

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        });

        return await GetCartAsync(userId, storeId);
    }

    // ───────────────────────────────────────────────────────────────────────
    // QUITAR / CAMBIAR CANTIDAD
    // ───────────────────────────────────────────────────────────────────────

    public async Task<CartViewDto> RemoveItemAsync(string userId, int cartItemId, string itemType)
    {
        RequireUser(userId);

        var storeId = await GuardAsync("quitar artículo", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var cart = await FindActiveCartAsync(db, userId);
            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado.");

            switch ((itemType ?? string.Empty).ToLowerInvariant())
            {
                case "product":
                    var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId);
                    if (item != null) { cart.CartItems.Remove(item); db.CartItems.Remove(item); }
                    break;
                case "combo":
                    var combo = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId);
                    if (combo != null) { cart.CartCombos.Remove(combo); db.CartCombos.Remove(combo); }
                    break;
                case "promotion":
                    var promo = cart.CartPromotions.FirstOrDefault(cp => cp.CartPromotionId == cartItemId);
                    if (promo != null) { cart.CartPromotions.Remove(promo); db.CartPromotions.Remove(promo); }
                    break;
                default:
                    throw new InvalidOperationException("Tipo de artículo no válido.");
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return cart.StoreId;
        });

        return await GetCartAsync(userId, storeId);
    }

    public async Task<CartViewDto> UpdateItemQuantityAsync(string userId, int cartItemId, int newQuantity, string itemType)
    {
        RequireUser(userId);

        if (newQuantity <= 0)
            return await RemoveItemAsync(userId, cartItemId, itemType);

        RequireQuantity(newQuantity);

        var storeId = await GuardAsync("cambiar cantidad", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var cart = await FindActiveCartAsync(db, userId);
            if (cart == null)
                throw new InvalidOperationException("Carrito no encontrado.");

            MergeDuplicateLines(db, cart);

            var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart));
            var names = NamesOf(cart);
            IReadOnlyCollection<int> affected;

            switch ((itemType ?? string.Empty).ToLowerInvariant())
            {
                case "product":
                    var item = cart.CartItems.FirstOrDefault(ci => ci.CartItemId == cartItemId)
                               ?? throw new InvalidOperationException("Ese artículo ya no está en tu carrito.");
                    item.Quantity = newQuantity;
                    item.SubTotal = newQuantity * item.UnitPrice;
                    affected = new[] { item.ProductId };
                    break;

                case "combo":
                    var combo = cart.CartCombos.FirstOrDefault(cc => cc.CartComboId == cartItemId)
                                ?? throw new InvalidOperationException("Ese combo ya no está en tu carrito.");
                    combo.Quantity = newQuantity;
                    combo.SubTotal = newQuantity * combo.UnitPrice;
                    affected = PerUnitOfCombo(combo).Keys.ToList();
                    break;

                case "promotion":
                    var promo = cart.CartPromotions.FirstOrDefault(cp => cp.CartPromotionId == cartItemId)
                                ?? throw new InvalidOperationException("Esa promoción ya no está en tu carrito.");
                    promo.Quantity = newQuantity;
                    promo.SubTotal = newQuantity * promo.UnitPrice;
                    affected = PerUnitOfPromo(promo).Keys.ToList();
                    break;

                default:
                    throw new InvalidOperationException("Tipo de artículo no válido.");
            }

            EnsureStock(cart, affected, stock, names);

            cart.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return cart.StoreId;
        });

        return await GetCartAsync(userId, storeId);
    }

    public async Task<CartViewDto> UpdateItemSelectionAsync(string userId, int cartItemId, bool isSelected, string itemType)
    {
        RequireUser(userId);
        // La selección vive solo en la pantalla del carrito; no se guarda en la base.
        return await GetCartAsync(userId, DefaultStoreId);
    }

    public async Task<CartViewDto> SelectAllAsync(string userId)
    {
        RequireUser(userId);
        var cart = await GetCartAsync(userId, DefaultStoreId);
        cart.Products.ForEach(p => p.IsSelected = true);
        cart.Combos.ForEach(c => c.IsSelected = true);
        cart.Promotions.ForEach(p => p.IsSelected = true);
        return cart;
    }

    public async Task<CartViewDto> DeselectAllAsync(string userId)
    {
        RequireUser(userId);
        var cart = await GetCartAsync(userId, DefaultStoreId);
        cart.Products.ForEach(p => p.IsSelected = false);
        cart.Combos.ForEach(c => c.IsSelected = false);
        cart.Promotions.ForEach(p => p.IsSelected = false);
        return cart;
    }

    public async Task<CartViewDto> ClearCartAsync(string userId)
    {
        RequireUser(userId);

        var storeId = await GuardAsync("vaciar carrito", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await AcquireCartLockAsync(db, userId);

            var cart = await FindActiveCartAsync(db, userId);
            if (cart == null)
                return DefaultStoreId;

            // Antes no se vaciaban las promociones; ahora sí.
            db.CartItems.RemoveRange(cart.CartItems);
            db.CartCombos.RemoveRange(cart.CartCombos);
            db.CartPromotions.RemoveRange(cart.CartPromotions);
            cart.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return cart.StoreId;
        });

        return await GetCartAsync(userId, storeId);
    }

    // ───────────────────────────────────────────────────────────────────────
    // RECIBO
    // ───────────────────────────────────────────────────────────────────────

    public async Task<ReceiptDto> GenerateReceiptAsync(string userId, CheckoutSelectionDto? selection = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Intento de generar recibo con UserId nulo o vacío.");
            return null!;
        }

        return await GuardAsync("generar recibo", async () =>
        {
            await using var db = await _factory.CreateDbContextAsync();

            var userEmail = await db.Users
                .AsNoTracking()
                .Where(u => u.UserId == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            var (cartView, storeName) = await BuildViewAsync(userId, null);
            if (cartView.CartId == 0)
                throw new InvalidOperationException("Carrito no encontrado.");

            var localDateTime = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-4)).DateTime;
            var shortId = userId.Length >= 4 ? userId[..4] : userId;

            var receipt = new ReceiptDto
            {
                ReceiptNumber = $"REC-{localDateTime:yyyyMMddHHmmss}-{shortId}",
                CreatedAt = localDateTime,
                CustomerName = userEmail ?? "Cliente Registrado",
                StoreLocation = storeName
            };

            foreach (var product in cartView.Products.Where(p => selection == null || selection.ProductItemIds.Contains(p.CartItemId)))
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

            foreach (var combo in cartView.Combos.Where(c => selection == null || selection.ComboItemIds.Contains(c.CartComboId)))
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

            foreach (var promo in cartView.Promotions.Where(p => selection == null || selection.PromotionItemIds.Contains(p.CartPromotionId)))
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

            receipt.TotalAmount = receipt.Items.Sum(i => i.SubTotal);
            return receipt;
        });
    }

    // ───────────────────────────────────────────────────────────────────────
    // CHECKOUT — la parte que no puede fallar
    // ───────────────────────────────────────────────────────────────────────

    public async Task<bool> CheckoutAsync(string userId, CheckoutSelectionDto? selection = null)
    {
        RequireUser(userId);

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await CheckoutOnceAsync(userId, selection);
            }
            catch (SqlException ex) when (IsRetryable(ex) && attempt < MaxCheckoutAttempts)
            {
                // Deadlock o espera de candado: la transacción ya se revirtió completa,
                // así que reintentar desde cero es seguro.
                _logger.LogWarning(ex, "Checkout reintentado ({Attempt}/{Max}) para {UserId}", attempt, MaxCheckoutAttempts, userId);
                await Task.Delay(150 * attempt);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && IsRetryable(sql) && attempt < MaxCheckoutAttempts)
            {
                _logger.LogWarning(ex, "Checkout reintentado ({Attempt}/{Max}) para {UserId}", attempt, MaxCheckoutAttempts, userId);
                await Task.Delay(150 * attempt);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Checkout rechazado para {UserId}: {Message}", userId, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en checkout para {UserId}", userId);
                throw new InvalidOperationException(
                    "No se pudo completar la compra. No se descontó stock ni se creó ningún pedido. Intenta de nuevo.");
            }
        }
    }

    private async Task<bool> CheckoutOnceAsync(string userId, CheckoutSelectionDto? selection)
    {
        await using var db = await _factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        // Un solo checkout a la vez por cliente (doble clic, dos pestañas).
        await AcquireCartLockAsync(db, userId);

        var cart = await CartQuery(db)
            .Where(c => c.UserId == userId && c.CartStatus == "Active")
            .OrderBy(c => c.CartId)
            .FirstOrDefaultAsync();

        if (cart == null)
            return false;

        MergeDuplicateLines(db, cart);

        var items = cart.CartItems.Where(ci => selection == null || selection.ProductItemIds.Contains(ci.CartItemId)).ToList();
        var combos = cart.CartCombos.Where(cc => selection == null || selection.ComboItemIds.Contains(cc.CartComboId)).ToList();
        var promos = cart.CartPromotions.Where(cp => selection == null || selection.PromotionItemIds.Contains(cp.CartPromotionId)).ToList();

        if (items.Count + combos.Count + promos.Count == 0)
            return false;

        var now = DateTime.Now;

        // ── 1. Todo lo que se va a comprar tiene que seguir vigente y con cantidades válidas ──
        foreach (var ci in items)
        {
            if (ci.Quantity < 1)
                throw new InvalidOperationException("Hay una cantidad no válida en tu carrito. Revísalo e intenta de nuevo.");
            if (ci.Product == null || !ci.Product.IsActive)
                throw new InvalidOperationException($"'{ci.Product?.DisplayName ?? "Un producto"}' ya no está disponible. Revisa tu carrito.");
        }

        foreach (var cc in combos)
        {
            if (cc.Quantity < 1)
                throw new InvalidOperationException("Hay una cantidad no válida en tu carrito. Revísalo e intenta de nuevo.");
            var combo = cc.Combo;
            if (combo == null || !combo.IsActive || !InDateRange(combo.StartDate, combo.EndDate, now) || combo.ComboProducts.Count == 0)
                throw new InvalidOperationException($"El combo «{combo?.Name ?? "seleccionado"}» ya no está disponible. Revisa tu carrito.");
            var inactive = combo.ComboProducts.FirstOrDefault(cp => cp.Product == null || !cp.Product.IsActive);
            if (inactive != null)
                throw new InvalidOperationException($"El combo «{combo.Name}» ya no se puede armar. Revisa tu carrito.");
        }

        foreach (var cp in promos)
        {
            if (cp.Quantity < 1)
                throw new InvalidOperationException("Hay una cantidad no válida en tu carrito. Revísalo e intenta de nuevo.");
            var promo = cp.Promotion;
            if (promo == null || !promo.IsActive || promo.StartDate > now || promo.EndDate < now || promo.PromotionProducts.Count == 0)
                throw new InvalidOperationException($"La promoción «{promo?.Title ?? "seleccionada"}» ya no está disponible. Revisa tu carrito.");
            var inactive = promo.PromotionProducts.FirstOrDefault(pp => pp.Product == null || !pp.Product.IsActive);
            if (inactive != null)
                throw new InvalidOperationException($"La promoción «{promo.Title}» ya no se puede aplicar. Revisa tu carrito.");
        }

        // ── 2. Cuánto necesita CADA bebida en total (suelto + combos + promos) ──
        var demand = DemandOf(items, combos, promos);
        var stock = await LoadStockAsync(db, cart.StoreId, demand.Keys);

        var names = NamesOf(cart);

        // El precio mostrado al cliente tiene que ser el precio real de hoy.
        foreach (var ci in items)
        {
            if (!stock.TryGetValue(ci.ProductId, out var row))
                throw new InvalidOperationException($"'{NameOf(names, ci.ProductId)}' no está disponible en esta tienda.");
            if (row.Price != ci.UnitPrice)
                throw new InvalidOperationException($"El precio de '{NameOf(names, ci.ProductId)}' cambió. Revisa tu carrito antes de comprar.");
        }
        foreach (var cc in combos)
        {
            if (cc.UnitPrice != cc.Combo.ComboPrice)
                throw new InvalidOperationException($"El precio del combo «{cc.Combo.Name}» cambió. Revisa tu carrito antes de comprar.");
        }

        // ── 3. DESCUENTO ATÓMICO DE STOCK ──
        // Orden fijo por ProductId para que dos compras que se crucen nunca se bloqueen entre sí.
        foreach (var (productId, needed) in demand.OrderBy(kv => kv.Key))
        {
            if (!stock.ContainsKey(productId))
                throw new InvalidOperationException($"'{NameOf(names, productId)}' no está disponible en esta tienda.");

            var qty = (int)Math.Min(needed, int.MaxValue);

            // La condición "Stock >= qty" se evalúa DENTRO del UPDATE, con la fila bloqueada.
            // No hay ventana entre "leer" y "descontar" en la que otro cliente pueda colarse.
            var affected = await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.ProductStores
                   SET Stock = Stock - {qty},
                       IsAvailable = CASE WHEN Stock - {qty} > 0 THEN 1 ELSE 0 END
                 WHERE ProductId = {productId}
                   AND StoreId   = {cart.StoreId}
                   AND Stock    >= {qty};");

            if (affected != 1)
            {
                var current = await db.ProductStores.AsNoTracking()
                    .Where(ps => ps.ProductId == productId && ps.StoreId == cart.StoreId)
                    .Select(ps => ps.Stock)
                    .FirstOrDefaultAsync();

                var name = NameOf(names, productId);
                throw new InvalidOperationException(current <= 0
                    ? $"'{name}' se agotó justo ahora. No se realizó ninguna compra; revisa tu carrito."
                    : $"Ya solo quedan {current} de '{name}' y tu compra necesita {qty}. No se realizó ninguna compra; ajusta tu carrito.");
            }

            db.StockMovements.Add(new StockMovement
            {
                ProductId = productId,
                StoreId = cart.StoreId,
                Quantity = qty,
                MovementType = "Out",
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        // ── 4. Pedido ──
        var total = items.Sum(i => i.Quantity * i.UnitPrice)
                  + combos.Sum(c => c.Quantity * c.UnitPrice)
                  + promos.Sum(p => p.Quantity * p.UnitPrice);

        var order = new Order
        {
            UserId = userId,
            StoreId = cart.StoreId,
            OrderStatus = "Completed",
            PaymentMethod = "WhatsApp",
            TotalAmount = total,
            CreatedAt = DateTime.Now
        };

        // OrderItems solo para productos sueltos (los combos y promociones quedan
        // registrados por los StockMovements, igual que antes).
        foreach (var ci in items)
        {
            order.OrderItems.Add(new OrderItem
            {
                ProductId = ci.ProductId,
                Quantity = ci.Quantity,
                UnitPrice = ci.UnitPrice
            });
        }
        db.Orders.Add(order);

        // ── 5. Cerrar lo comprado ──
        var boughtEverything = items.Count == cart.CartItems.Count
                               && combos.Count == cart.CartCombos.Count
                               && promos.Count == cart.CartPromotions.Count;

        if (boughtEverything)
        {
            cart.CartStatus = "Sent";
            cart.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Compra parcial: lo comprado pasa a un carrito "Sent" (queda el historial)
            // y lo no comprado sigue activo en el carrito del cliente.
            var sentCart = new Cart
            {
                UserId = userId,
                StoreId = cart.StoreId,
                CartStatus = "Sent",
                CreatedAt = cart.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };
            db.Carts.Add(sentCart);
            await db.SaveChangesAsync();

            foreach (var ci in items) ci.CartId = sentCart.CartId;
            foreach (var cc in combos) cc.CartId = sentCart.CartId;
            foreach (var cp in promos) cp.CartId = sentCart.CartId;

            cart.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        _logger.LogInformation("Checkout completado para UserId={UserId}. OrderId={OrderId}.", userId, order.OrderId);
        return true;
    }

    // ───────────────────────────────────────────────────────────────────────
    // AYUDAS INTERNAS
    // ───────────────────────────────────────────────────────────────────────

    private async Task<T> GuardAsync<T>(string what, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException)
        {
            // Mensajes de negocio ya redactados para el cliente.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en carrito al {What}", what);
            throw new InvalidOperationException("No pudimos completar esa acción en tu carrito. Intenta de nuevo.");
        }
    }

    private static bool IsRetryable(SqlException ex) => ex.Number is 1205 or 1222;

    private static void RequireUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new InvalidOperationException("Tu sesión no es válida. Inicia sesión de nuevo.");
    }

    private static void RequireQuantity(int quantity)
    {
        if (quantity < 1 || quantity > MaxQuantityPerLine)
            throw new InvalidOperationException("La cantidad no es válida.");
    }

    private static bool InDateRange(DateTime? start, DateTime? end, DateTime now) =>
        (!start.HasValue || start.Value <= now) && (!end.HasValue || end.Value >= now);

    // Candado por cliente que vive lo que dure la transacción.
    private static async Task AcquireCartLockAsync(ApplicationDbContext db, string userId)
    {
        var resourceName = $"licors-cart:{userId}";
        if (resourceName.Length > 200)
            resourceName = "licors-cart:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)));

        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resourceName };

        await db.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;",
            result, resource);

        if (result.Value is not int code || code < 0)
            throw new InvalidOperationException("Tu carrito se está procesando en este momento. Intenta de nuevo en unos segundos.");
    }

    private static IQueryable<Cart> CartQuery(ApplicationDbContext db) =>
        db.Carts
            .Include(c => c.Store)
            .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
            .Include(c => c.CartCombos).ThenInclude(cc => cc.Combo)
                .ThenInclude(cb => cb.ComboProducts).ThenInclude(cp => cp.Product)
            .Include(c => c.CartPromotions).ThenInclude(cp => cp.Promotion)
                .ThenInclude(p => p.PromotionProducts).ThenInclude(pp => pp.Product)
            .AsSplitQuery();

    private static Task<Cart?> FindActiveCartAsync(ApplicationDbContext db, string userId) =>
        CartQuery(db)
            .Where(c => c.UserId == userId && c.CartStatus == "Active")
            .OrderBy(c => c.CartId)
            .FirstOrDefaultAsync();

    private static async Task<Cart> GetOrCreateActiveCartAsync(ApplicationDbContext db, string userId, int storeId)
    {
        var cart = await CartQuery(db)
            .Where(c => c.UserId == userId && c.StoreId == storeId && c.CartStatus == "Active")
            .OrderBy(c => c.CartId)
            .FirstOrDefaultAsync();

        if (cart != null)
            return cart;

        cart = new Cart
        {
            UserId = userId,
            StoreId = storeId,
            CartStatus = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return cart;
    }

    // Si por datos viejos hay filas repetidas del mismo producto/combo/promoción,
    // se unen en una sola (las cantidades se suman).
    private static void MergeDuplicateLines(ApplicationDbContext db, Cart cart)
    {
        foreach (var g in cart.CartItems.GroupBy(ci => ci.ProductId).Where(g => g.Count() > 1).ToList())
        {
            var keep = g.OrderBy(ci => ci.CartItemId).First();
            foreach (var dup in g.Where(ci => !ReferenceEquals(ci, keep)).ToList())
            {
                keep.Quantity += dup.Quantity;
                cart.CartItems.Remove(dup);
                db.CartItems.Remove(dup);
            }
            keep.SubTotal = keep.Quantity * keep.UnitPrice;
        }

        foreach (var g in cart.CartCombos.GroupBy(cc => cc.ComboId).Where(g => g.Count() > 1).ToList())
        {
            var keep = g.OrderBy(cc => cc.CartComboId).First();
            foreach (var dup in g.Where(cc => !ReferenceEquals(cc, keep)).ToList())
            {
                keep.Quantity += dup.Quantity;
                cart.CartCombos.Remove(dup);
                db.CartCombos.Remove(dup);
            }
            keep.SubTotal = keep.Quantity * keep.UnitPrice;
        }

        foreach (var g in cart.CartPromotions.GroupBy(cp => cp.PromotionId).Where(g => g.Count() > 1).ToList())
        {
            var keep = g.OrderBy(cp => cp.CartPromotionId).First();
            foreach (var dup in g.Where(cp => !ReferenceEquals(cp, keep)).ToList())
            {
                keep.Quantity += dup.Quantity;
                cart.CartPromotions.Remove(dup);
                db.CartPromotions.Remove(dup);
            }
            keep.SubTotal = keep.Quantity * keep.UnitPrice;
        }
    }

    // Stock y precio reales leídos AHORA de la base (sin tracking → nunca datos viejos).
    private static async Task<Dictionary<int, ProductStore>> LoadStockAsync(ApplicationDbContext db, int storeId, IEnumerable<int> productIds)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<int, ProductStore>();

        return await db.ProductStores
            .AsNoTracking()
            .Where(ps => ps.StoreId == storeId && ids.Contains(ps.ProductId))
            .ToDictionaryAsync(ps => ps.ProductId);
    }

    // Unidades de cada bebida que consume UNA unidad de la línea.
    private static Dictionary<int, int> PerUnitOfCombo(CartCombo cc) =>
        (cc.Combo?.ComboProducts ?? Enumerable.Empty<ComboProduct>())
            .GroupBy(cp => cp.ProductId)
            .ToDictionary(g => g.Key, g => Math.Max(g.Sum(x => x.Quantity), 1));

    // Una promoción consume 1 unidad por cada unidad de promoción de cada producto que incluye.
    private static Dictionary<int, int> PerUnitOfPromo(CartPromotion cp) =>
        (cp.Promotion?.PromotionProducts ?? Enumerable.Empty<PromotionProduct>())
            .Select(pp => pp.ProductId)
            .Distinct()
            .ToDictionary(id => id, _ => 1);

    private static IEnumerable<int> ProductIdsOf(Cart cart)
    {
        foreach (var ci in cart.CartItems)
            yield return ci.ProductId;
        foreach (var cc in cart.CartCombos)
            foreach (var id in PerUnitOfCombo(cc).Keys)
                yield return id;
        foreach (var cp in cart.CartPromotions)
            foreach (var id in PerUnitOfPromo(cp).Keys)
                yield return id;
    }

    // Total de unidades que necesita cada bebida sumando suelto + combos + promociones.
    private static Dictionary<int, long> DemandOf(IEnumerable<CartItem> items, IEnumerable<CartCombo> combos, IEnumerable<CartPromotion> promos)
    {
        var demand = new Dictionary<int, long>();

        void Add(int productId, long qty)
        {
            demand[productId] = demand.TryGetValue(productId, out var current) ? current + qty : qty;
        }

        foreach (var ci in items)
            Add(ci.ProductId, ci.Quantity);

        foreach (var cc in combos)
            foreach (var kv in PerUnitOfCombo(cc))
                Add(kv.Key, (long)cc.Quantity * kv.Value);

        foreach (var cp in promos)
            foreach (var kv in PerUnitOfPromo(cp))
                Add(kv.Key, (long)cp.Quantity * kv.Value);

        return demand;
    }

    private static Dictionary<int, string> NamesOf(Cart cart)
    {
        var names = new Dictionary<int, string>();

        foreach (var ci in cart.CartItems)
            if (ci.Product != null) names[ci.ProductId] = ci.Product.DisplayName;

        foreach (var cc in cart.CartCombos)
            foreach (var cp in cc.Combo?.ComboProducts ?? Enumerable.Empty<ComboProduct>())
                if (cp.Product != null) names[cp.ProductId] = cp.Product.DisplayName;

        foreach (var cp in cart.CartPromotions)
            foreach (var pp in cp.Promotion?.PromotionProducts ?? Enumerable.Empty<PromotionProduct>())
                if (pp.Product != null) names[pp.ProductId] = pp.Product.DisplayName;

        return names;
    }

    private static string NameOf(Dictionary<int, string> names, int productId) =>
        names.TryGetValue(productId, out var name) ? name : $"producto #{productId}";

    // Lanza si, con el carrito tal como quedó en memoria, alguna de las bebidas
    // afectadas necesita más unidades de las que hay en stock.
    private static void EnsureStock(Cart cart, IEnumerable<int> affectedProductIds, IReadOnlyDictionary<int, ProductStore> stock, Dictionary<int, string> names)
    {
        var demand = DemandOf(cart.CartItems, cart.CartCombos, cart.CartPromotions);

        foreach (var productId in affectedProductIds.Distinct())
        {
            demand.TryGetValue(productId, out var needed);
            var have = stock.TryGetValue(productId, out var row) ? Math.Max(row.Stock, 0) : 0;

            if (needed <= have)
                continue;

            var name = NameOf(names, productId);

            if (have <= 0)
                throw new InvalidOperationException($"'{name}' está agotado por el momento.");

            throw new InvalidOperationException(
                $"No contamos con esa cantidad de '{name}'. Disponible: {have}; con lo que llevas en tu carrito necesitarías {needed}.");
        }
    }

    // Vista de solo lectura del carrito + stock real de cada línea.
    private async Task<(CartViewDto View, string StoreName)> BuildViewAsync(string userId, int? storeId)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var cart = await CartQuery(db)
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.CartStatus == "Active" && (storeId == null || c.StoreId == storeId))
            .OrderBy(c => c.CartId)
            .FirstOrDefaultAsync();

        if (cart == null)
            return (new CartViewDto(), string.Empty);

        var stock = await LoadStockAsync(db, cart.StoreId, ProductIdsOf(cart));

        int StockOf(int productId) => stock.TryGetValue(productId, out var r) ? Math.Max(r.Stock, 0) : 0;
        int MaxUnits(Dictionary<int, int> perUnit) =>
            perUnit.Count == 0 ? 0 : perUnit.Min(kv => StockOf(kv.Key) / Math.Max(kv.Value, 1));

        var result = new CartViewDto { CartId = cart.CartId };

        foreach (var item in cart.CartItems)
        {
            result.Products.Add(new CartItemDisplayDto
            {
                CartItemId = item.CartItemId,
                ProductId = item.ProductId,
                ProductName = item.Product.DisplayName,
                ProductImage = item.Product.ImageUrl,
                Category = "Producto",
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                IsSelected = false,
                AvailableStock = StockOf(item.ProductId)
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
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                IsSelected = false,
                AvailableStock = MaxUnits(PerUnitOfCombo(item))
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
                IsSelected = false,
                AvailableStock = MaxUnits(PerUnitOfPromo(item))
            });
        }

        return (result, cart.Store?.Name ?? string.Empty);
    }
}
