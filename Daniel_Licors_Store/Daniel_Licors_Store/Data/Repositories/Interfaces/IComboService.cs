using Daniel_Licors_Store.Models.Combo;

namespace Daniel_Licors_Store.Data.Services.Interfaces
{
    public interface IComboService
    {
        Task<bool> CreateComboAsync(ComboCreateModel model);
        Task<List<ComboAdminDto>> GetAdminCombosAsync();
        Task<List<ComboDto>> GetActiveCombosAsync();
        Task<ComboDetailDto?> GetComboDetailAsync(int comboId);
        Task<List<ComboDto>> GetCombosByProductAsync(int productId);
        Task<bool> ValidateComboStockAsync(int comboId, int storeId, int quantity);
        Task<bool> ConsumeComboStockAsync(int comboId, int storeId, int quantity, string? userId);
    }
}