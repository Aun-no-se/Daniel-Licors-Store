using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class CategoryBrandService
    {
        private readonly ApplicationDbContext _context;

        public CategoryBrandService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── CATEGORÍAS ───────────────────────────────────────────────────────

        public async Task<List<Category>> GetAllCategoriesAsync()
            => await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

        public async Task<Category?> GetCategoryByIdAsync(int id)
            => await _context.Categories.FindAsync(id);

        public async Task<(bool Ok, string Error)> CreateCategoryAsync(string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "El nombre no puede estar vacío.");

            var exists = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
            if (exists)
                return (false, $"Ya existe una categoría con el nombre \"{name}\".");

            _context.Categories.Add(new Category { Name = name, IsActive = true });
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> UpdateCategoryAsync(int id, string name, bool isActive)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "El nombre no puede estar vacío.");

            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return (false, "Categoría no encontrada.");

            var dup = await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.CategoryId != id);
            if (dup)
                return (false, $"Ya existe otra categoría con el nombre \"{name}\".");

            cat.Name = name;
            cat.IsActive = isActive;
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> DeleteCategoryAsync(int id)
        {
            var cat = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null) return (false, "Categoría no encontrada.");

            if (cat.Products.Any())
                return (false, $"No se puede eliminar: la categoría tiene {cat.Products.Count} producto(s) asignado(s). Desactívela en su lugar.");

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        // ── MARCAS ───────────────────────────────────────────────────────────

        public async Task<List<Brand>> GetAllBrandsAsync()
            => await _context.Brands
                .OrderBy(b => b.Name)
                .ToListAsync();

        public async Task<Brand?> GetBrandByIdAsync(int id)
            => await _context.Brands.FindAsync(id);

        public async Task<(bool Ok, string Error)> CreateBrandAsync(string name)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "El nombre no puede estar vacío.");

            var exists = await _context.Brands
                .AnyAsync(b => b.Name.ToLower() == name.ToLower());
            if (exists)
                return (false, $"Ya existe una marca con el nombre \"{name}\".");

            _context.Brands.Add(new Brand { Name = name, IsActive = true });
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> UpdateBrandAsync(int id, string name, bool isActive)
        {
            name = name.Trim();
            if (string.IsNullOrWhiteSpace(name))
                return (false, "El nombre no puede estar vacío.");

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return (false, "Marca no encontrada.");

            var dup = await _context.Brands
                .AnyAsync(b => b.Name.ToLower() == name.ToLower() && b.BrandId != id);
            if (dup)
                return (false, $"Ya existe otra marca con el nombre \"{name}\".");

            brand.Name = name;
            brand.IsActive = isActive;
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Ok, string Error)> DeleteBrandAsync(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.BrandId == id);

            if (brand == null) return (false, "Marca no encontrada.");

            if (brand.Products.Any())
                return (false, $"No se puede eliminar: la marca tiene {brand.Products.Count} producto(s) asignado(s). Desactívela en su lugar.");

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }
    }
}
