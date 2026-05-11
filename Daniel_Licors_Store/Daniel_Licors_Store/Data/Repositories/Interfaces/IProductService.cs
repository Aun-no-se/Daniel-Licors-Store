using Daniel_Licors_Store.Models.Product;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces
{
    public interface IProductService
    {
        Task<int> CreateProductAsync(ProductCreateModel model);
    }
}
