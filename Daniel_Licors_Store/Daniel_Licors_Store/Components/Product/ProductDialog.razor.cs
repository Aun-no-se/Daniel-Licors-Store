using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Product;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Daniel_Licors_Store.Components.Product;

public partial class ProductDialog
{

    [Inject] private ICatalogService CatalogService { get; set; } = default!;
    [Inject] private IWebHostEnvironment Env { get; set; } = default!;
    [Inject] private ProductStateService ProductState { get; set; } = default!;
    [Inject] private IProductService ProductService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public int? ProductId { get; set; }

    private ProductEditModel Model = new();

    private List<Category> categories = new();
    private List<Brand> brands = new();

    private bool isLoading = true;
    private bool _loaded = false;
    private bool isSaving = false;
    private bool isDragOver = false;
    private bool imageError = false;

    private MudForm form = default!;
    private bool formIsValid;

    private IBrowserFile? selectedImage;
    private byte[]? _validatedImageBytes;   // ya validados por firma real (magic number)
    private string? _validatedImageExtension;
    private string? previewImage;

    private bool HasImage => !string.IsNullOrEmpty(previewImage ?? Model.ImageUrl);
    private string? _currentUserId;

    // ── Tamaño ──────────────────────────────────────────────────────
    private bool sizeError = false;
    private bool customSizeOpen = false;
    private string customSize = string.Empty;
    private bool keepAddingSizes = false;   // "Otro tamaño": seguir en el formulario al guardar

    // Los botones de tamaño dependen de la categoría elegida
    private SizeRule SizeRuleNow
        => ProductSizeCatalog.GetRule(categories.FirstOrDefault(c => c.CategoryId == Model.CategoryId)?.Name);

    // Detecta un tamaño escrito dentro del nombre (ej. "Brugal Añejo 750ml", "Presidente 12 oz")
    private static readonly Regex VolumeInName = new(
        @"\b\d+([.,]\d+)?\s*(ml|cl|lts?|l|oz|onzas?|litros?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    // ── Init ────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        if (_loaded) return;
        _loaded = true;
        isLoading = true;

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _currentUserId = authState.User.FindFirst("UserId")?.Value
                      ?? authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        categories = await CatalogService.GetCategoriesAsync();
        brands = await CatalogService.GetBrandsAsync();

        if (ProductId.HasValue)
        {
            var product = await ProductService.GetProductForEditAsync(ProductId.Value);

            Model = new ProductEditModel
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                CategoryId = product.CategoryId,
                BrandId = product.BrandId,
                AgingYears = product.AgingYears,
                Price = product.Price,
                Stock = product.Stock,
                OriginalStock = product.Stock,
                IsPremium = product.IsPremium,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl,
                Size = product.Size,
                SizeMl = product.SizeMl
            };

            previewImage = null;
        }

        // Si la bebida tiene un tamaño que no está en los botones, se muestra como "Otro…"
        if (!string.IsNullOrWhiteSpace(Model.Size) && !SizeRuleNow.Contains(Model.Size))
        {
            customSizeOpen = true;
            customSize = Model.Size!;
        }


        isLoading = false;
    }

    // ── Validaciones (campos obligatorios) ─────────────────────────
    private string? ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "El nombre es obligatorio";
        if (name.Trim().Length < 3)
            return "El nombre debe tener al menos 3 caracteres";

        // El tamaño se elige con los botones y se agrega solo: no debe ir dentro del nombre
        if (!string.IsNullOrWhiteSpace(Model.Size) && VolumeInName.IsMatch(name))
            return "Quita el tamaño del nombre: se agrega solo con los botones de tamaño";
        return null;
    }

    private string? ValidateCategory(int categoryId)
        => categoryId <= 0 ? "Selecciona una categoría" : null;

    private string? ValidatePrice(decimal price)
        => price <= 0 ? "El precio debe ser mayor a 0" : null;

    private string? ValidateStock(int stock)
        => stock < 0 ? "El stock no puede ser negativo" : null;

    private string? ValidateAging(int? aging)
        => (!aging.HasValue || aging.Value < 0) ? "Ingresa los años de añejamiento (0 si no se añeja)" : null;

    // ── Tamaño: acciones de los botones ─────────────────────────────
    private bool IsPresetSelected(SizeOption option)
        => !customSizeOpen && string.Equals(Model.Size, option.Label, StringComparison.OrdinalIgnoreCase);

    private void SelectSize(SizeOption option)
    {
        var wasSelected = IsPresetSelected(option);
        customSizeOpen = false;
        customSize = string.Empty;

        if (wasSelected)
        {
            // tocar el mismo botón otra vez lo quita
            Model.Size = null;
            Model.SizeMl = null;
            return;
        }

        Model.Size = option.Label;
        Model.SizeMl = option.Ml;
        sizeError = false;
    }

    private void ToggleCustomSize()
    {
        customSizeOpen = !customSizeOpen;

        if (customSizeOpen)
        {
            Model.Size = ProductSizeCatalog.NormalizeLabel(customSize);
            Model.SizeMl = ProductSizeCatalog.TryParseMl(Model.Size);
        }
        else
        {
            customSize = string.Empty;
            Model.Size = null;
            Model.SizeMl = null;
        }
    }

    private void OnCustomSizeChanged()
    {
        Model.Size = ProductSizeCatalog.NormalizeLabel(customSize);
        Model.SizeMl = ProductSizeCatalog.TryParseMl(Model.Size);
        if (Model.Size != null) sizeError = false;
    }

    // Al cambiar de categoría cambian los botones: si el tamaño elegido no existe en la
    // nueva categoría se limpia, para no guardar un tamaño que no le corresponde.
    private void OnCategoryChanged()
    {
        if (!customSizeOpen && Model.Size != null && !SizeRuleNow.Contains(Model.Size))
        {
            Model.Size = null;
            Model.SizeMl = null;
        }
        sizeError = false;
    }

    // Después de crear con «Otro tamaño» activo: se limpia solo lo que cambia entre
    // tamaños (tamaño, precio, stock). Nombre, categoría, marca, descripción e imagen se
    // conservan, y la imagen ya guardada se reutiliza.
    private void PrepareNextSize()
    {
        _validatedImageBytes = null;
        _validatedImageExtension = null;
        selectedImage = null;

        Model.Size = null;
        Model.SizeMl = null;
        Model.Price = 0;
        Model.Stock = 0;

        customSizeOpen = false;
        customSize = string.Empty;
        sizeError = false;
    }


    // ── Actions ─────────────────────────────────────────────────────
    private void Cancelar() => MudDialog.Cancel();

    private async Task SaveProduct()
    {
        if (isSaving) return;

        await form.Validate();

        imageError = !HasImage;

        sizeError = SizeRuleNow.Required && string.IsNullOrWhiteSpace(Model.Size);
        Model.Size = ProductSizeCatalog.NormalizeLabel(Model.Size);
        Model.SizeMl ??= ProductSizeCatalog.TryParseMl(Model.Size);

        if (!formIsValid || imageError || sizeError)
        {
            Snackbar.Add("Completa todos los campos obligatorios antes de guardar.", Severity.Warning);
            StateHasChanged();
            return;
        }

        isSaving = true;

        try
        {
            if (_validatedImageBytes != null)
                Model.ImageUrl = await SaveImageAsync();

            if (ProductId.HasValue)
            {
                await ProductService.UpdateProductAsync(Model, _currentUserId);
            }
            else
            {
                Model.IsActive = true;

                var createModel = new ProductCreateModel
                {
                    Name = Model.Name,
                    Description = Model.Description,
                    CategoryId = Model.CategoryId,
                    BrandId = Model.BrandId,
                    AgingYears = Model.AgingYears,
                    Price = Model.Price,
                    Stock = Model.Stock,
                    IsPremium = Model.IsPremium,
                    IsActive = Model.IsActive,
                    ImageFileName = Model.ImageUrl,
                    Size = Model.Size,
                    SizeMl = Model.SizeMl
                };

                await ProductService.CreateProductAsync(createModel, _currentUserId);

                if (keepAddingSizes)
                {
                    // Se queda en el formulario para cargar el siguiente tamaño de la misma bebida
                    ProductState.NotifyChange();
                    Snackbar.Add($"«{ProductNaming.Compose(Model.Name, Model.Size)}» creada. Elige el siguiente tamaño.", Severity.Success);
                    PrepareNextSize();
                    return;
                }
            }

            Snackbar.Add(ProductId == null ? "Producto creado correctamente." : "Cambios guardados correctamente.", Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
            ProductState.NotifyChange();
        }
        catch (InvalidOperationException ex)
        {
            // Mensajes pensados para el empleado (ej. ya existe esa bebida con ese tamaño)
            Snackbar.Add(ex.Message, Severity.Warning);
        }
        catch (Exception)
        {
            Snackbar.Add("Ocurrió un error al guardar el producto. Intenta de nuevo.", Severity.Error);
        }
        finally
        {
            isSaving = false;
        }
    }

    // ── Image handling ───────────────────────────────────────────────
    private void ClearImage()
    {
        previewImage = null;
        Model.ImageUrl = null;
        selectedImage = null;
        _validatedImageBytes = null;
        _validatedImageExtension = null;
    }

    private async Task OnImageSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null) return;

        using var stream = file.OpenReadStream(maxAllowedSize: ImageUploadValidator.MaxSizeBytes);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var bytes = ms.ToArray();

        // Validación real: firma de bytes, no nombre de archivo ni Content-Type
        // (antes este método no validaba nada — aceptaba cualquier archivo).
        var result = ImageUploadValidator.Validate(bytes, file.Size);
        if (!result.IsValid)
        {
            imageError = true;
            Snackbar.Add(result.ErrorMessage!, Severity.Warning);
            return;
        }

        selectedImage = file;
        _validatedImageBytes = bytes;
        _validatedImageExtension = result.Extension;
        previewImage = $"data:image/{result.Extension!.TrimStart('.')};base64,{Convert.ToBase64String(bytes)}";

        imageError = false;
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        isDragOver = false;
        // Drag-drop de archivos en Blazor Server requiere JS interop real;
        // el InputFile estándar cubre el flujo habitual.
        await Task.CompletedTask;
    }

    private async Task<string?> SaveImageAsync()
    {
        if (_validatedImageBytes == null || _validatedImageExtension == null) return null;

        var uploadsFolder = Path.Combine(Env.WebRootPath, "images", "products");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // La extensión sale de la firma de bytes validada en OnImageSelected,
        // nunca del nombre original del archivo.
        var fileName = $"{Guid.NewGuid()}{_validatedImageExtension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await File.WriteAllBytesAsync(filePath, _validatedImageBytes);

        return $"/images/products/{fileName}";
    }
}