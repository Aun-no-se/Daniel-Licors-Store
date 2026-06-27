using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDetailDto>> GetProductDetailsAsync();
        Task<int> CreateProductAsync(ProductCreateModel model);
        Task<int> CreateProductAsync(ProductCreateModel model, string? userId);
        Task<ProductEditModel?> GetProductForEditAsync(int productId);
        Task UpdateProductAsync(ProductEditModel model);
        Task UpdateProductAsync(ProductEditModel model, string? userId);
        Task DeleteProductAsync(int productId);
        Task DeleteProductAsync(int productId, string? userId);
    }
}
