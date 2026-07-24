using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Models.Promotion;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using System.Globalization;

namespace Daniel_Licors_Store.Components.Pages;

public partial class Promotions : AdminPageBase
{
    protected override string[] AllowedRoles => new[] { "Admin", "Empleado" };

    [Inject] public IPromotionService PromotionService { get; set; } = default!;
    [Inject] public IProductService ProductService { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public IWebHostEnvironment Env { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;

    // ─── Cultura española ────────────────────────────────────
    private static readonly CultureInfo _es = new CultureInfo("es-DO");

    // ─── Estado general ──────────────────────────────────────
    private List<PromotionAdminDto> _promos = new();
    private List<ProductDetailDto> _products = new();
    private List<PromotionProductLine> _promoBuilder = new();

    private bool _loading = true;
    private bool _showForm;
    private bool _isEditMode;
    private int _editingPromoId;

    private PromotionCreateModel _form = new();

    // ── Fechas como string para inputs nativos (evita bug removeChild de MudDatePicker en móvil)
    private string _startDateStr = DateTime.Today.ToString("yyyy-MM-dd");
    private string _endDateStr = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

    private DateTime StartDate => DateTime.TryParse(_startDateStr, out var d) ? d : DateTime.Today;
    private DateTime EndDate => DateTime.TryParse(_endDateStr, out var d) ? d : DateTime.Today.AddDays(7);

    // ─── Upload de imagen ────────────────────────────────────
    private bool _uploadingImage = false;
    private string _uploadError = string.Empty;
    private string? _previewBase64;      // preview inmediato en base64

    // ─── Mini-dialog de línea ────────────────────────────────
    private bool _showLineDialog;
    private ProductDetailDto? _lineDialogProduct;
    private decimal _lineDiscountPct;
    private decimal _lineSpecialPrice;
    private int _lineReqQty = 2;
    private int _linePayQty = 1;

    // ─── Referencia al ancla del formulario ─────────────────
    private ElementReference _formAnchor;

    // ── Helper para la imagen (base64 preview o URL guardada) ──────
    private bool HasPromoImage => !string.IsNullOrEmpty(_previewBase64 ?? _form.ImageUrl);

    // ════════════════════════════════════════════════════════
    // INIT — el chequeo de auth ya lo hizo AdminPageBase antes de
    // llegar aquí; esto es solo la carga de datos de la página.
    // ════════════════════════════════════════════════════════
    protected override async Task OnInitializedAuthorizedAsync()
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
        try { _promos = await PromotionService.GetAdminPromotionsAsync(); }
        catch (Exception ex) { Snackbar.Add($"Error cargando promociones: {ex.Message}", Severity.Error); }
        finally { _loading = false; }
    }

    // ════════════════════════════════════════════════════════
    // UPLOAD DE IMAGEN
    // ════════════════════════════════════════════════════════
    private async Task HandleImageUpload(InputFileChangeEventArgs e)
    {
        _uploadError = string.Empty;
        var file = e.File;
        if (file == null) return;

        _uploadingImage = true;
        StateHasChanged();

        try
        {
            // Tope duro de lectura — protege memoria aunque el tamaño
            // declarado por el navegador esté manipulado.
            await using var stream = file.OpenReadStream(ImageUploadValidator.MaxSizeBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // Validación real: firma de bytes, no nombre de archivo ni Content-Type
            // (ambos los controla quien sube el archivo y se falsifican fácil).
            var result = ImageUploadValidator.Validate(bytes, file.Size);
            if (!result.IsValid)
            {
                _uploadError = result.ErrorMessage!;
                return;
            }

            _previewBase64 = $"data:image/{result.Extension!.TrimStart('.')};base64,{Convert.ToBase64String(bytes)}";

            // Guardar en disco — la extensión sale de la firma de bytes
            // validada arriba, nunca del nombre original del archivo.
            var uploadDir = Path.Combine(Env.WebRootPath, "images", "promociones");
            Directory.CreateDirectory(uploadDir);
            var fileName = $"promo_{Guid.NewGuid():N}{result.Extension}";
            var fullPath = Path.Combine(uploadDir, fileName);

            await using var fs = new FileStream(fullPath, FileMode.Create);
            await fs.WriteAsync(bytes);

            _form.ImageUrl = $"/images/promociones/{fileName}";
        }
        catch (Exception ex) { _uploadError = $"Error al subir imagen: {ex.Message}"; _previewBase64 = null; }
        finally { _uploadingImage = false; StateHasChanged(); }
    }

    private void RemoveImage()
    {
        _form.ImageUrl = null;
        _previewBase64 = null;
        _uploadError = string.Empty;
    }

    // ════════════════════════════════════════════════════════
    // FORMULARIO
    // ════════════════════════════════════════════════════════
    private async Task ShowCreateSection()
    {
        ResetForm();
        _showForm = true;
        await ScrollToForm();
    }

    private async Task ScrollToForm()
    {
        await Task.Delay(80); // espera a que Blazor renderice el anchor
        try { await JS.InvokeVoidAsync("eval", "document.getElementById('promo-form-anchor')?.scrollIntoView({behavior:'smooth',block:'start'})"); }
        catch { /* ignorar si JS falla */ }
    }

    private void ResetForm()
    {
        _showForm = false;
        _isEditMode = false;
        _editingPromoId = 0;
        _form = new PromotionCreateModel();
        _promoBuilder.Clear();
        _startDateStr = DateTime.Today.ToString("yyyy-MM-dd");
        _endDateStr = DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");
        _uploadError = string.Empty;
        _previewBase64 = null;
    }

    private async Task SavePromo()
    {
        if (!_promoBuilder.Any()) { Snackbar.Add("Agrega al menos un producto a la promoción", Severity.Warning); return; }

        _form.StartDate = StartDate;
        _form.EndDate = EndDate;
        _form.Products = _promoBuilder;

        try
        {
            if (_isEditMode)
            {
                await PromotionService.UpdatePromotionAsync(_editingPromoId, _form, CurrentUserId);
                Snackbar.Add("Promoción actualizada correctamente", Severity.Success);
            }
            else
            {
                await PromotionService.CreatePromotionAsync(_form, CurrentUserId);
                Snackbar.Add("Promoción creada correctamente", Severity.Success);
            }
            ResetForm();
            await LoadPromos();
        }
        catch (Exception ex) { Snackbar.Add($"Error guardando promoción: {ex.Message}", Severity.Error); }
    }

    private async Task EditPromo(int id)
    {
        try
        {
            var promo = await PromotionService.GetPromotionByIdAsync(id);
            if (promo == null) { Snackbar.Add("Promoción no encontrada", Severity.Warning); return; }

            _isEditMode = true;
            _editingPromoId = id;
            _showForm = true;

            _form = new PromotionCreateModel
            {
                Title = promo.Title,
                Description = promo.Description,
                PromotionType = promo.PromotionType ?? "Discount",
                IsActive = promo.IsActive,
                ImageUrl = promo.ImageUrl
            };
            _previewBase64 = null;
            _startDateStr = promo.StartDate.ToString("yyyy-MM-dd");
            _endDateStr = promo.EndDate.ToString("yyyy-MM-dd");

            _promoBuilder.Clear();
            foreach (var pp in promo.PromotionProducts)
            {
                var product = _products.FirstOrDefault(p => p.ProductId == pp.ProductId);
                _promoBuilder.Add(new PromotionProductLine
                {
                    ProductId = pp.ProductId,
                    ProductName = product?.Name ?? $"Producto #{pp.ProductId}",
                    OriginalPrice = product?.Price ?? 0,
                    DiscountPercentage = pp.DiscountPercentage,
                    SpecialPrice = pp.SpecialPrice,
                    RequiredQuantity = pp.RequiredQuantity,
                    PayQuantity = pp.PayQuantity
                });
            }
            await ScrollToForm();
        }
        catch (Exception ex) { Snackbar.Add($"Error cargando promoción: {ex.Message}", Severity.Error); }
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

        try { await PromotionService.DeletePromotionAsync(id, CurrentUserId); Snackbar.Add("Promoción eliminada", Severity.Success); await LoadPromos(); }
        catch (Exception ex) { Snackbar.Add($"Error eliminando: {ex.Message}", Severity.Error); }
    }

    // ════════════════════════════════════════════════════════
    // MINI-DIALOG LÍNEA
    // ════════════════════════════════════════════════════════
    private void OpenPromoLineDialog(ProductDetailDto product)
    {
        _lineDialogProduct = product;
        _lineDiscountPct = 0;
        _lineSpecialPrice = product.Price;
        _lineReqQty = 3;
        _linePayQty = 2;
        _showLineDialog = true;
    }

    private void CloseLineDialog() { _showLineDialog = false; _lineDialogProduct = null; }

    private void ConfirmLineDialog()
    {
        if (_lineDialogProduct == null) return;
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
            case "Discount": line.DiscountPercentage = _lineDiscountPct; break;
            case "SpecialPrice": line.SpecialPrice = _lineSpecialPrice; break;
            case "PayXGetY": line.RequiredQuantity = _lineReqQty; line.PayQuantity = _linePayQty; break;
        }
        _promoBuilder.Add(line);
        CloseLineDialog();
    }

    // ════════════════════════════════════════════════════════
    // HELPERS UI
    // ════════════════════════════════════════════════════════
    private string FormatDate(DateTime d, string fmt = "dd MMM yyyy") => d.ToString(fmt, _es);

    private Color GetTypeColor(string? type) => type switch
    {
        "Discount" => Color.Error,
        "SpecialPrice" => Color.Warning,
        "PayXGetY" => Color.Success,
        _ => Color.Info
    };

    private string GetTypeLabel(string? type) => type switch
    {
        "Discount" => "% Descuento",
        "SpecialPrice" => "Precio Especial",
        "PayXGetY" => "Lleva X Paga Y",
        _ => type ?? "Promo"
    };

    private string GetTypeChipClass(string? type) => type switch
    {
        "Discount" => "pr-chip-discount",
        "SpecialPrice" => "pr-chip-specialprice",
        "PayXGetY" => "pr-chip-payxgety",
        _ => "pr-chip-other"
    };

    private string GetLineLabel(PromotionProductLine line)
    {
        if (line.DiscountPercentage.HasValue) return $"{line.ProductName} — {line.DiscountPercentage:0.##}% OFF";
        if (line.SpecialPrice.HasValue) return $"{line.ProductName} — ${line.SpecialPrice:N2}";
        if (line.RequiredQuantity.HasValue) return $"{line.ProductName} — Lleva {line.RequiredQuantity} Paga {line.PayQuantity}";
        return line.ProductName;
    }
}
