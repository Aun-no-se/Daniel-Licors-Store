using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly ApplicationDbContext _context;

        public CatalogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Category>> GetCategoriesAsync()
            => await _context.Categories.Where(x => x.IsActive).ToListAsync();

        public async Task<List<Brand>> GetBrandsAsync()
            => await _context.Brands.Where(x => x.IsActive).ToListAsync();
    }
}
