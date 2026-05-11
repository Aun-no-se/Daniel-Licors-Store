using Daniel_Licors_Store.Data.Entities;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces
{
    public interface ICatalogService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Brand>> GetBrandsAsync();
    }
}
