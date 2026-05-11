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

            // 🔥 IMPORTANTE: relaciones explícitas
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
    }
}
