using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Admin;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages;

public partial class AdminDashboard
{
    [Inject] private AdminDashboardService DashboardService { get; set; } = default!;
    [Inject] private CategoryBrandService CatalogMgmt { get; set; } = default!;
    [Inject] private HiddenItemsService HiddenItems { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    // ── Auth ─────────────────────────────────────────────────────────────────
    bool isAdmin = false;

    // ── Dashboard (solo Admin) ────────────────────────────────────────────────
    bool loading = true;
    DashboardSummary summary = new();
    ChartData salesChart = new();
    ChartData categoryChart = new();
    List<TopProductDto> topProducts = new();
    List<RecentOrderDto> recentOrders = new();

    ChartOptions lineOptions = new ChartOptions
    {
        YAxisTicks = 5,
        LineStrokeWidth = 2.5f
    };

    // ── Catálogo (Admin y Empleado) ───────────────────────────────────────────
    int catalogTab = 0;
    List<Category> categories = new();
    List<Brand> brands = new();

    string newCategoryName = string.Empty;
    string newBrandName = string.Empty;
    bool catalogSaving = false;

    // Edición inline
    int? editingCategoryId = null;
    int? editingBrandId = null;
    string editCatalogName = string.Empty;
    bool editCatalogActive = true;

    // ── Elementos Ocultos (Admin y Empleado) ──────────────────────────────────
    int hiddenTab = 0; // 0 = Productos, 1 = Combos, 2 = Promociones
    bool hiddenLoading = true;
    bool hiddenActionBusy = false;
    List<HiddenProductDto> hiddenProducts = new();
    List<HiddenComboDto> hiddenCombos = new();
    List<HiddenPromotionDto> hiddenPromotions = new();
    string? _currentUserId;

    // ─────────────────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = authState.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            Nav.NavigateTo("/login", replace: true);
            return;
        }

        isAdmin = user.IsInRole("Admin");
        bool isEmpleado = user.IsInRole("Empleado");

        if (!isAdmin && !isEmpleado)
        {
            Nav.NavigateTo("/", replace: true);
            return;
        }

        _currentUserId = user.FindFirst("UserId")?.Value
                      ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        loading = true;
        try
        {
            categories = await CatalogMgmt.GetAllCategoriesAsync();
            brands = await CatalogMgmt.GetAllBrandsAsync();

            await LoadHiddenItemsAsync();

            if (isAdmin)
            {
                summary = await DashboardService.GetSummaryAsync();
                salesChart = await DashboardService.GetSalesChartAsync(30);
                categoryChart = await DashboardService.GetCategoryChartAsync();
                topProducts = await DashboardService.GetTopProductsAsync(8);
                recentOrders = await DashboardService.GetRecentOrdersAsync(10);
            }
        }
        finally
        {
            loading = false;
        }
    }

    // ── Helpers de Dashboard ─────────────────────────────────────────────────

    List<ChartSeries> GetSalesChartSeries()
    {
        if (salesChart.Data.Length == 0) return new();
        return new List<ChartSeries>
        {
            new ChartSeries { Name = "Ventas RD$", Data = salesChart.Data[0] }
        };
    }

    string StatusLabel(string status) => status switch
    {
        "Pending" => "Pendiente",
        "InProcess" => "En proceso",
        "Completed" => "Completado",
        _ => status
    };

    // ── Tabs ─────────────────────────────────────────────────────────────────

    void SwitchCatalogTab(int tab)
    {
        catalogTab = tab;
        CancelEdit();
    }

    // ── Categorías ───────────────────────────────────────────────────────────

    async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(newCategoryName)) return;
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.CreateCategoryAsync(newCategoryName);
            if (ok)
            {
                Snackbar.Add($"Categoría \"{newCategoryName}\" creada.", Severity.Success);
                newCategoryName = string.Empty;
                categories = await CatalogMgmt.GetAllCategoriesAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    void StartEditCategory(Category cat)
    {
        CancelEdit();
        editingCategoryId = cat.CategoryId;
        editCatalogName = cat.Name;
        editCatalogActive = cat.IsActive;
    }

    async Task SaveCategoryEdit()
    {
        if (!editingCategoryId.HasValue) return;
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.UpdateCategoryAsync(
                editingCategoryId.Value, editCatalogName, editCatalogActive);
            if (ok)
            {
                Snackbar.Add("Categoría actualizada.", Severity.Success);
                CancelEdit();
                categories = await CatalogMgmt.GetAllCategoriesAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    async Task DeleteCategoryAsync(Category cat)
    {
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.DeleteCategoryAsync(cat.CategoryId);
            if (ok)
            {
                Snackbar.Add($"Categoría \"{cat.Name}\" eliminada.", Severity.Success);
                categories = await CatalogMgmt.GetAllCategoriesAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    // ── Marcas ───────────────────────────────────────────────────────────────

    async Task AddBrandAsync()
    {
        if (string.IsNullOrWhiteSpace(newBrandName)) return;
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.CreateBrandAsync(newBrandName);
            if (ok)
            {
                Snackbar.Add($"Marca \"{newBrandName}\" creada.", Severity.Success);
                newBrandName = string.Empty;
                brands = await CatalogMgmt.GetAllBrandsAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    void StartEditBrand(Brand brand)
    {
        CancelEdit();
        editingBrandId = brand.BrandId;
        editCatalogName = brand.Name;
        editCatalogActive = brand.IsActive;
    }

    async Task SaveBrandEdit()
    {
        if (!editingBrandId.HasValue) return;
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.UpdateBrandAsync(
                editingBrandId.Value, editCatalogName, editCatalogActive);
            if (ok)
            {
                Snackbar.Add("Marca actualizada.", Severity.Success);
                CancelEdit();
                brands = await CatalogMgmt.GetAllBrandsAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    async Task DeleteBrandAsync(Brand brand)
    {
        catalogSaving = true;
        try
        {
            var (ok, error) = await CatalogMgmt.DeleteBrandAsync(brand.BrandId);
            if (ok)
            {
                Snackbar.Add($"Marca \"{brand.Name}\" eliminada.", Severity.Success);
                brands = await CatalogMgmt.GetAllBrandsAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        finally { catalogSaving = false; }
    }

    void CancelEdit()
    {
        editingCategoryId = null;
        editingBrandId = null;
        editCatalogName = string.Empty;
        editCatalogActive = true;
    }

    // ── Elementos Ocultos ────────────────────────────────────────────────────

    async Task LoadHiddenItemsAsync()
    {
        hiddenLoading = true;
        try
        {
            hiddenProducts = await HiddenItems.GetHiddenProductsAsync();
            hiddenCombos = await HiddenItems.GetHiddenCombosAsync();
            hiddenPromotions = await HiddenItems.GetHiddenPromotionsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando elementos ocultos: {ex.Message}", Severity.Error);
        }
        finally
        {
            hiddenLoading = false;
        }
    }

    int TotalHiddenCount() => hiddenProducts.Count + hiddenCombos.Count + hiddenPromotions.Count;

    string GetHiddenPromoTypeLabel(string? type) => type switch
    {
        "Discount" => "% Descuento",
        "SpecialPrice" => "Precio Especial",
        "PayXGetY" => "Lleva X Paga Y",
        _ => type ?? "Promo"
    };

    async Task RestoreProductAsync(int productId)
    {
        if (hiddenActionBusy) return;
        hiddenActionBusy = true;
        try
        {
            var (ok, error) = await HiddenItems.RestoreProductAsync(productId, _currentUserId);
            if (ok)
            {
                Snackbar.Add("Producto visible de nuevo ✓", Severity.Success);
                hiddenProducts = await HiddenItems.GetHiddenProductsAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally { hiddenActionBusy = false; }
    }

    async Task RestoreComboAsync(int comboId)
    {
        if (hiddenActionBusy) return;
        hiddenActionBusy = true;
        try
        {
            var (ok, error) = await HiddenItems.RestoreComboAsync(comboId, _currentUserId);
            if (ok)
            {
                Snackbar.Add("Combo visible de nuevo ✓", Severity.Success);
                hiddenCombos = await HiddenItems.GetHiddenCombosAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally { hiddenActionBusy = false; }
    }

    async Task RestorePromotionAsync(int promotionId)
    {
        if (hiddenActionBusy) return;
        hiddenActionBusy = true;
        try
        {
            var (ok, error) = await HiddenItems.RestorePromotionAsync(promotionId, _currentUserId);
            if (ok)
            {
                Snackbar.Add("Promoción visible de nuevo ✓", Severity.Success);
                hiddenPromotions = await HiddenItems.GetHiddenPromotionsAsync();
            }
            else
            {
                Snackbar.Add(error, Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally { hiddenActionBusy = false; }
    }
}
