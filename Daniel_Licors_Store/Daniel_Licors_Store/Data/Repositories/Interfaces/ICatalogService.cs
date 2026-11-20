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

        // Una sección por cada categoría y marca activa (con sus productos), en el mismo
        // orden que la barra infinita: primero categorías A→Z, luego marcas A→Z.
        Task<List<CatalogSectionModel>> GetCatalogSectionsAsync();
        Task<List<ComboDto>> GetCombosByProductAsync(int productId);

        // Devuelve promociones activas de un producto con todos los campos del DTO
        Task<List<PromotionDto>> GetPromotionsByProductAsync(int productId);
    }
}
