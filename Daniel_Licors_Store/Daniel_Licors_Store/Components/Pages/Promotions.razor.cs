using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Models.Promotion;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages;

public partial class Promotions
{
    [Inject] public IPromotionService PromotionService { get; set; } = default!;
    [Inject] public IProductService ProductService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public IWebHostEnvironment Env { get; set; } = default!;

    // ─── Estado general ───────────────────────────────────
    private List<PromotionAdminDto> _promos = new();
    private List<ProductDetailDto> _products = new();
    private List<PromotionProductLine> _promoBuilder = new();

    private bool _loading = true;
    private bool _showForm;
    private bool _isEditMode;
    private int _editingPromoId;

    private PromotionCreateModel _form = new();

    // Fechas separadas para los MudDatePicker (que trabajan con DateTime?)
    private DateTime? _startDate = DateTime.Today;
    private DateTime? _endDate = DateTime.Today.AddDays(7);

    // ─── Estado del mini-dialog de línea ──────────────────
    private bool _showLineDialog;
    private ProductDetailDto? _lineDialogProduct;

    private decimal _lineDiscountPct;
    private decimal _lineSpecialPrice;
    private int _lineReqQty = 2;
    private int _linePayQty = 1;

    // ══════════════════════════════════════════════════════
    // INIT
    // ══════════════════════════════════════════════════════
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _products = await ProductService.GetProductDetailsAsync();
            await LoadPromos();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando datos: {ex.Message}", Severity.Error);
        }
    }

    private async Task LoadPromos()
    {
        _loading = true;
        try
        {
            _promos = await PromotionService.GetAdminPromotionsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando promociones: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    // ══════════════════════════════════════════════════════
    // FORMULARIO
    // ══════════════════════════════════════════════════════
    private void ShowCreateSection()
    {
        ResetForm();
        _showForm = true;
    }

    private void ResetForm()
    {
        _showForm = false;
        _isEditMode = false;
        _editingPromoId = 0;
        _form = new PromotionCreateModel();
        _promoBuilder.Clear();
        _startDate = DateTime.Today;
        _endDate = DateTime.Today.AddDays(7);
    }

    private async Task SavePromo()
    {
        if (!_promoBuilder.Any())
        {
            Snackbar.Add("Agrega al menos un producto a la promoción", Severity.Warning);
            return;
        }

        _form.StartDate = _startDate ?? DateTime.Today;
        _form.EndDate = _endDate ?? DateTime.Today.AddDays(7);
        _form.Products = _promoBuilder;

        try
        {
            if (_isEditMode)
            {
                await PromotionService.UpdatePromotionAsync(_editingPromoId, _form);
                Snackbar.Add("Promoción actualizada correctamente", Severity.Success);
            }
            else
            {
                await PromotionService.CreatePromotionAsync(_form);
                Snackbar.Add("Promoción creada correctamente", Severity.Success);
            }

            ResetForm();
            await LoadPromos();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error guardando promoción: {ex.Message}", Severity.Error);
        }
    }

    private async Task EditPromo(int id)
    {
        try
        {
            var promo = await PromotionService.GetPromotionByIdAsync(id);
            if (promo == null)
            {
                Snackbar.Add("Promoción no encontrada", Severity.Warning);
                return;
            }

            _isEditMode = true;
            _editingPromoId = id;
            _showForm = true;

            _form = new PromotionCreateModel
            {
                Title = promo.Title,
                Description = promo.Description,
                PromotionType = promo.PromotionType ?? "Discount",
                IsActive = promo.IsActive
            };

            _startDate = promo.StartDate;
            _endDate = promo.EndDate;

            // Para edición cargamos el ProductsSummary como referencia visual,
            // el usuario re-agrega los productos si quiere cambiarlos.
            _promoBuilder.Clear();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando promoción: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeletePromo(int id)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "¿Seguro que deseas eliminar esta promoción? Esta acción no se puede deshacer.",
            ["ButtonText"] = "Eliminar",
            ["ButtonColor"] = Color.Error
        };

        var dialog = DialogService.Show<ConfirmDialog>("Confirmar eliminación", parameters);
        var result = await dialog.Result;

        if (result.Canceled) return;

        try
        {
            await PromotionService.DeletePromotionAsync(id);
            Snackbar.Add("Promoción eliminada", Severity.Success);
            await LoadPromos();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error eliminando: {ex.Message}", Severity.Error);
        }
    }

    // ══════════════════════════════════════════════════════
    // MINI-DIALOG LÍNEA DE PRODUCTO
    // ══════════════════════════════════════════════════════
    private void OpenPromoLineDialog(ProductDetailDto product)
    {
        _lineDialogProduct = product;
        _lineDiscountPct = 0;
        _lineSpecialPrice = product.Price;
        _lineReqQty = 3;
        _linePayQty = 2;
        _showLineDialog = true;
    }

    private void CloseLineDialog()
    {
        _showLineDialog = false;
        _lineDialogProduct = null;
    }

    private void ConfirmLineDialog()
    {
        if (_lineDialogProduct == null) return;

        // Si ya existe, lo reemplaza
        var existing = _promoBuilder.FirstOrDefault(x => x.ProductId == _lineDialogProduct.ProductId);
        if (existing != null) _promoBuilder.Remove(existing);

        var line = new PromotionProductLine
        {
            ProductId = _lineDialogProduct.ProductId,
            ProductName = _lineDialogProduct.Name,
            OriginalPrice = _lineDialogProduct.Price
        };

        switch (_form.PromotionType)
        {
            case "Discount":
                line.DiscountPercentage = _lineDiscountPct;
                break;
            case "SpecialPrice":
                line.SpecialPrice = _lineSpecialPrice;
                break;
            case "PayXGetY":
                line.RequiredQuantity = _lineReqQty;
                line.PayQuantity = _linePayQty;
                break;
        }

        _promoBuilder.Add(line);
        CloseLineDialog();
    }

    // ══════════════════════════════════════════════════════
    // HELPERS UI
    // ══════════════════════════════════════════════════════
    private Color GetTypeColor(string? type) => type switch
    {
        "Discount"     => Color.Error,
        "SpecialPrice" => Color.Warning,
        "PayXGetY"     => Color.Success,
        _              => Color.Info
    };

    private string GetTypeLabel(string? type) => type switch
    {
        "Discount"     => "% Descuento",
        "SpecialPrice" => "Precio Especial",
        "PayXGetY"     => "Lleva X Paga Y",
        _              => type ?? "Promo"
    };

    private string GetLineLabel(PromotionProductLine line)
    {
        if (line.DiscountPercentage.HasValue)
            return $"{line.ProductName} — {line.DiscountPercentage:0.##}% OFF";
        if (line.SpecialPrice.HasValue)
            return $"{line.ProductName} — ${line.SpecialPrice:N2}";
        if (line.RequiredQuantity.HasValue)
            return $"{line.ProductName} — Lleva {line.RequiredQuantity} Paga {line.PayQuantity}";
        return line.ProductName;
    }
}
