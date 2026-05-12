using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Data.Entities;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces
{
    public interface ICatalogService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Brand>> GetBrandsAsync();

        Task<List<ProductResponseModel>> GetProductsAsync();
        Task<List<ProductResponseModel>> GetProductsByCategoryAsync(int categoryId);
        Task<bool> CreateProductAsync(ProductCreateModel model);
    }
}