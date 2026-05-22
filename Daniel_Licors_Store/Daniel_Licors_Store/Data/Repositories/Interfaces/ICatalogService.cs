using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces
{
    public interface ICatalogService
    {
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Brand>> GetBrandsAsync();
        Task<ProductDetailDto?> GetProductDetailAsync(int productId);
        Task<List<ProductResponseModel>> GetProductsAsync();
        Task<List<ProductResponseModel>> GetProductsByCategoryAsync(int categoryId);
        Task<List<ComboDto>> GetCombosByProductAsync(int productId);

        Task<List<PromotionDto>> GetPromotionsByProductAsync(int productId);

    }
}