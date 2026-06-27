using Daniel_Licors_Store.Models.Promotion;

namespace Daniel_Licors_Store.Data.Repositories.Interfaces;

public interface IPromotionService
{
    Task<List<PromotionAdminDto>> GetAdminPromotionsAsync();
    Task<bool> CreatePromotionAsync(PromotionCreateModel model);
    Task<bool> CreatePromotionAsync(PromotionCreateModel model, string? userId);
    Task UpdatePromotionAsync(int promotionId, PromotionCreateModel model);
    Task UpdatePromotionAsync(int promotionId, PromotionCreateModel model, string? userId);
    Task DeletePromotionAsync(int promotionId);
    Task DeletePromotionAsync(int promotionId, string? userId);
    Task<PromotionAdminDto?> GetPromotionByIdAsync(int promotionId);
}
