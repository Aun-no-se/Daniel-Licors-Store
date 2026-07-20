using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
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
    private string? previewImage;

    private bool HasImage => !string.IsNullOrEmpty(previewImage ?? Model.ImageUrl);
    private string? _currentUserId;

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
                IsPremium = product.IsPremium,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl
            };

            previewImage = null;
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
        return null;
    }

    private string? ValidateCategory(int categoryId)
        => categoryId <= 0 ? "Selecciona una categoría" : null;

    private string? ValidatePrice(decimal price)
        => price <= 0 ? "El precio debe ser mayor a 0" : null;

    private string? ValidateStock(int stock)
        => stock < 0 ? "El stock no puede ser negativo" : null;

    private string? ValidateAging(int? aging)
        => (!aging.HasValue || aging.Value <= 0) ? "Ingresa los años de añejamiento" : null;

    // ── Actions ─────────────────────────────────────────────────────
    private void Cancelar() => MudDialog.Cancel();

    private async Task SaveProduct()
    {
        if (isSaving) return;

        await form.Validate();

        imageError = !HasImage;

        if (!formIsValid || imageError)
        {
            Snackbar.Add("Completa todos los campos obligatorios antes de guardar.", Severity.Warning);
            StateHasChanged();
            return;
        }

        isSaving = true;

        try
        {
            if (selectedImage != null)
                Model.ImageUrl = await SaveImageAsync(selectedImage);

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
                    ImageFileName = Model.ImageUrl
                };

                await ProductService.CreateProductAsync(createModel, _currentUserId);
            }

            Snackbar.Add(ProductId == null ? "Producto creado correctamente." : "Cambios guardados correctamente.", Severity.Success);

            MudDialog.Close(DialogResult.Ok(true));
            ProductState.NotifyChange();
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
    }

    private async Task OnImageSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file is null) return;

        selectedImage = file;
        using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        previewImage = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";

        imageError = false;
    }

    private async Task HandleDrop(DragEventArgs e)
    {
        isDragOver = false;
        // Drag-drop de archivos en Blazor Server requiere JS interop real;
        // el InputFile estándar cubre el flujo habitual.
        await Task.CompletedTask;
    }

    private async Task<string?> SaveImageAsync(IBrowserFile file)
    {
        if (file is null) return null;

        var uploadsFolder = Path.Combine(Env.WebRootPath, "images", "products");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.Name)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using var stream = file.OpenReadStream(10 * 1024 * 1024);
        using var fileStream = new FileStream(filePath, FileMode.Create);
        await stream.CopyToAsync(fileStream);

        return $"/images/products/{fileName}";
    }
}
