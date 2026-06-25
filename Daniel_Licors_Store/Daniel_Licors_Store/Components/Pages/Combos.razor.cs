using Daniel_Licors_Store.Components.Dialogs;
using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Combos
    {
        [Inject] public IComboService ComboService { get; set; } = default!;
        [Inject] public IProductService ProductService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IDialogService DialogService { get; set; } = default!;
        [Inject] public IWebHostEnvironment Env { get; set; } = default!;
        [Inject] public NavigationManager Nav { get; set; } = default!;
        [Inject] public IJSRuntime JS { get; set; } = default!;

        private List<ProductDetailDto> _products = new();
        private List<ComboAdminDto> _adminCombos = new();
        private List<ComboProductTemp> _comboBuilder = new();

        private bool _loading = true;
        private bool _showCreateCombo;
        private bool _isEditMode;
        private int _editingComboId;

        private byte[]? _imageBytes;
        private string? _imageFileName;
        private string? _previewBase64;   // preview inmediato

        private ComboCreateModel _newCombo = new();

        // ════════════════════════════════════════════════════════
        // INIT
        // ════════════════════════════════════════════════════════
        protected override async Task OnInitializedAsync()
        {
            try
            {
                _products = await ProductService.GetProductDetailsAsync();
                await LoadCombos();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error cargando datos: {ex.Message}", Severity.Error);
            }
        }

        private async Task LoadCombos()
        {
            _loading = true;
            try { _adminCombos = await ComboService.GetAdminCombosAsync(); }
            catch (Exception ex) { Snackbar.Add($"Error cargando combos: {ex.Message}", Severity.Error); }
            finally { _loading = false; }
        }

        // ════════════════════════════════════════════════════════
        // FORMULARIO
        // ════════════════════════════════════════════════════════
        private async Task ShowCreateSection()
        {
            ResetForm();
            _showCreateCombo = true;
            await ScrollToForm();
        }

        private async Task ScrollToForm()
        {
            await Task.Delay(80);
            try { await JS.InvokeVoidAsync("eval", "document.getElementById('combo-form-anchor')?.scrollIntoView({behavior:'smooth',block:'start'})"); }
            catch { /* ignorar si JS falla en el primer render */ }
        }

        private void ResetForm()
        {
            _showCreateCombo = false;
            _comboBuilder.Clear();
            _imageBytes = null;
            _imageFileName = null;
            _previewBase64 = null;
            _newCombo = new ComboCreateModel();
            _isEditMode = false;
            _editingComboId = 0;
        }

        private void RemoveImage()
        {
            _imageBytes = null;
            _imageFileName = null;
            _previewBase64 = null;
            _newCombo.ImageUrl = null;
        }

        // ════════════════════════════════════════════════════════
        // UPLOAD DE IMAGEN
        // ════════════════════════════════════════════════════════
        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file is null || file.Size <= 0) return;

                if (file.Size > 5 * 1024 * 1024)
                {
                    Snackbar.Add("La imagen no puede superar 5MB", Severity.Warning);
                    return;
                }

                var buffer = new byte[file.Size];
                await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
                var totalRead = 0;
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(totalRead, buffer.Length - totalRead))) > 0)
                    totalRead += bytesRead;

                _imageBytes = buffer[..totalRead];
                _imageFileName = file.Name;
                _previewBase64 = $"data:{file.ContentType};base64,{Convert.ToBase64String(_imageBytes)}";

                Snackbar.Add("Imagen cargada correctamente", Severity.Success);
                StateHasChanged();
            }
            catch (JSException jsEx) { Console.WriteLine($"[InputFile] JSException ignorada: {jsEx.Message}"); }
            catch (TaskCanceledException) { }
            catch (Exception ex) { Snackbar.Add($"Error leyendo imagen: {ex.Message}", Severity.Error); }
        }

        private async Task<string> SaveImageAsync()
        {
            var folder = Path.Combine(Env.WebRootPath, "images", "combos");
            Directory.CreateDirectory(folder);
            var extension = Path.GetExtension(_imageFileName ?? ".jpg");
            var fileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(folder, fileName);
            await File.WriteAllBytesAsync(fullPath, _imageBytes!);
            return $"/images/combos/{fileName}";
        }

        // ════════════════════════════════════════════════════════
        // CRUD
        // ════════════════════════════════════════════════════════
        private async Task SaveCombo()
        {
            if (!_comboBuilder.Any()) { Snackbar.Add("Agrega al menos un producto al combo", Severity.Warning); return; }

            try
            {
                if (_imageBytes != null)
                    _newCombo.ImageUrl = await SaveImageAsync();

                _newCombo.Products = _comboBuilder;

                if (_isEditMode)
                {
                    await ComboService.UpdateComboAsync(_editingComboId, _newCombo);
                    Snackbar.Add("Combo actualizado correctamente", Severity.Success);
                }
                else
                {
                    await ComboService.CreateComboAsync(_newCombo);
                    Snackbar.Add("Combo creado correctamente", Severity.Success);
                }
                ResetForm();
                await LoadCombos();
            }
            catch (Exception ex) { Snackbar.Add($"Error guardando combo: {ex.Message}", Severity.Error); }
        }

        private async Task EditCombo(int id)
        {
            try
            {
                var combo = await ComboService.GetComboDetailAsync(id);
                if (combo == null) { Snackbar.Add("Combo no encontrado", Severity.Warning); return; }

                _isEditMode = true;
                _editingComboId = id;
                _showCreateCombo = true;

                _newCombo = new ComboCreateModel
                {
                    Name = combo.Name,
                    Description = combo.Description,
                    ComboPrice = combo.ComboPrice,
                    ImageUrl = combo.ImageUrl != "/images/combo-default.png" ? combo.ImageUrl : null
                };
                _imageBytes = null;
                _imageFileName = null;
                _previewBase64 = null;

                _comboBuilder = combo.Products.Select(p => new ComboProductTemp
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Quantity = p.Quantity
                }).ToList();

                await ScrollToForm();
            }
            catch (Exception ex) { Snackbar.Add($"Error cargando combo: {ex.Message}", Severity.Error); }
        }

        private async Task DeleteCombo(int id)
        {
            var parameters = new DialogParameters
            {
                ["ContentText"] = "¿Seguro que deseas eliminar este combo? Esta acción no se puede deshacer.",
                ["ButtonText"] = "Eliminar",
                ["ButtonColor"] = Color.Error
            };
            var dialog = DialogService.Show<ConfirmDialog>("Confirmar eliminación", parameters);
            var result = await dialog.Result;
            if (result.Canceled) return;

            try { await ComboService.DeleteComboAsync(id); Snackbar.Add("Combo eliminado correctamente", Severity.Success); await LoadCombos(); }
            catch (Exception ex) { Snackbar.Add($"Error eliminando combo: {ex.Message}", Severity.Error); }
        }

        // ════════════════════════════════════════════════════════
        // MODAL CANTIDAD
        // ════════════════════════════════════════════════════════
        private async Task OpenQuantityModal(ProductDetailDto product)
        {
            var dialog = DialogService.Show<QuantityDialog>(
                "Cantidad",
                new DialogParameters { ["SelectedProduct"] = product }
            );
            var result = await dialog.Result;
            if (!result.Canceled && result.Data is int qty && qty > 0)
                AddProduct(product, qty);
        }

        private void AddProduct(ProductDetailDto product, int qty)
        {
            var existing = _comboBuilder.FirstOrDefault(x => x.ProductId == product.ProductId);
            if (existing != null)
                existing.Quantity += qty;
            else
                _comboBuilder.Add(new ComboProductTemp
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    Quantity = qty,
                    ImageUrl = product.ImageUrl ?? string.Empty
                });
        }
    }
}
