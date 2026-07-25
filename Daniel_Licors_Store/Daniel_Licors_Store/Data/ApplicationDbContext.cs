using Microsoft.EntityFrameworkCore;
using Daniel_Licors_Store.Data.Entities;

namespace Daniel_Licors_Store.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ============================
    // DbSets (solo lo necesario)
    // ============================
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStore> ProductStores => Set<ProductStore>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Combo> Combos => Set<Combo>();
    public DbSet<ComboProduct> ComboProducts => Set<ComboProduct>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<PromotionProduct> PromotionProducts => Set<PromotionProduct>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartCombo> CartCombos => Set<CartCombo>();
    public DbSet<CartPromotion> CartPromotions => Set<CartPromotion>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ============================
        // CATEGORY
        // ============================
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(e => e.CategoryId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.IsActive)
                  .IsRequired();

            entity.HasMany(e => e.Products)
                  .WithOne(p => p.Category)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // BRAND
        // ============================
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("Brands");

            entity.HasKey(e => e.BrandId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(e => e.IsActive)
                  .IsRequired();
        });

        // ============================
        // STORE
        // ============================
        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Stores");

            entity.HasKey(e => e.StoreId);

            entity.Property(e => e.Name)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(e => e.Address)
                  .HasMaxLength(250)
                  .IsRequired();

            //relaciones explícitas
            entity.HasMany(s => s.ProductStores)
                  .WithOne(ps => ps.Store)
                  .HasForeignKey(ps => ps.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Carts)
                  .WithOne(c => c.Store)
                  .HasForeignKey(c => c.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(s => s.Orders)
                  .WithOne(o => o.Store)
                  .HasForeignKey(o => o.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // PRODUCT
        // ============================
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            entity.HasKey(e => e.ProductId);

            entity.Property(e => e.Name)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(500);

            entity.Property(e => e.AgingYears);

            entity.Property(e => e.ImageUrl)
                  .HasMaxLength(500);

            entity.Property(e => e.IsPremium).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();

            entity.Property(e => e.CreatedAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(p => p.Brand)
                  .WithMany(b => b.Products)
                  .HasForeignKey(p => p.BrandId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // PRODUCT STORE
        // ============================
        modelBuilder.Entity<ProductStore>(entity =>
        {
            entity.ToTable("ProductStores");

            entity.HasKey(e => e.ProductStoreId);

            entity.Property(e => e.Price)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.Property(e => e.Stock).IsRequired();
            entity.Property(e => e.IsAvailable).IsRequired();

            entity.Property(e => e.RowVersion)
                  .IsRowVersion();

            entity.HasIndex(e => new { e.ProductId, e.StoreId })
                  .IsUnique();

            entity.HasOne(ps => ps.Product)
                  .WithMany(p => p.ProductStores)
                  .HasForeignKey(ps => ps.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ps => ps.Store)
                  .WithMany(s => s.ProductStores)
                  .HasForeignKey(ps => ps.StoreId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // USER REGISTERS
        // ============================
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.UserId);

            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
        });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(ur => ur.UserRoleId);

            entity.HasOne(ur => ur.User)
                  .WithMany(u => u.UserRoles)
                  .HasForeignKey(ur => ur.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ur => ur.Role)
                  .WithMany(r => r.UserRoles)
                  .HasForeignKey(ur => ur.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasKey(r => r.RoleId);

            entity.Property(r => r.Name)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(r => r.IsActive)
                  .IsRequired();
        });

        modelBuilder.Entity<Combo>(entity =>
        {
            entity.ToTable("Combos");

            entity.HasKey(e => e.ComboId);

            entity.Property(e => e.Name)
                  .HasMaxLength(150)
                  .IsRequired();

            entity.Property(e => e.ComboPrice)
                  .HasColumnType("decimal(10,2)");

            entity.Property(e => e.ImageUrl)
                  .HasMaxLength(500);

            entity.HasMany(c => c.ComboProducts)
                  .WithOne(cp => cp.Combo)
                  .HasForeignKey(cp => cp.ComboId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ============================
        // COMBO PRODUCT (Corregido Definitivo)
        // ============================
        modelBuilder.Entity<ComboProduct>(entity =>
        {
            entity.ToTable("ComboProducts");

            entity.HasKey(e => e.ComboProductId);

            entity.Property(e => e.Quantity)
                  .IsRequired();

            // Relación con Combo
            entity.HasOne(cp => cp.Combo)
                  .WithMany(c => c.ComboProducts)
                  .HasForeignKey(cp => cp.ComboId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relación con Product 
            entity.HasOne(cp => cp.Product)
                  .WithMany()
                  .HasForeignKey(cp => cp.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // CART
        // ============================
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");
            entity.HasKey(c => c.CartId);

            entity.Property(c => c.UserId)
                  .IsRequired()
                  .HasMaxLength(450);  // ✅ IMPORTANTE

            entity.Property(c => c.CartStatus)
                  .HasMaxLength(50)
                  .IsRequired();

            entity.Property(c => c.CreatedAt)
                  .HasColumnType("datetime")
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(c => c.UpdatedAt)
                  .HasColumnType("datetime");
        });


        // ============================
        // CART ITEM (PRODUCT)
        // ============================
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(ci => ci.CartItemId);

            entity.Property(ci => ci.Quantity)
                  .IsRequired();

            entity.Property(ci => ci.UnitPrice)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.Property(ci => ci.SubTotal)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.HasOne(ci => ci.Product)
                  .WithMany()
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });


        // ============================
        // CART COMBO
        // ============================
        modelBuilder.Entity<CartCombo>(entity =>
        {
            entity.ToTable("CartCombos");

            entity.HasKey(cc => cc.CartComboId);

            entity.Property(cc => cc.Quantity)
                  .IsRequired();

            entity.Property(cc => cc.UnitPrice)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.Property(cc => cc.SubTotal)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.HasOne(cc => cc.Combo)
                  .WithMany()
                  .HasForeignKey(cc => cc.ComboId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================
        // CART PROMOTION
        // ============================
        modelBuilder.Entity<CartPromotion>(entity =>
        {
            entity.ToTable("CartPromotions");

            entity.HasKey(cp => cp.CartPromotionId);

            entity.Property(cp => cp.Quantity)
                  .IsRequired();

            entity.Property(cp => cp.UnitPrice)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.Property(cp => cp.SubTotal)
                  .HasColumnType("decimal(10,2)")
                  .IsRequired();

            entity.HasOne(cp => cp.Cart)
                  .WithMany(c => c.CartPromotions)
                  .HasForeignKey(cp => cp.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cp => cp.Promotion)
                  .WithMany()
                  .HasForeignKey(cp => cp.PromotionId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
