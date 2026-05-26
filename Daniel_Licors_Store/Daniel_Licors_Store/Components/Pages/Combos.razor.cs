using Daniel_Licors_Store.Components.Dialogs;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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

        List<ProductDetailDto> _products = new();
        List<ComboAdminDto> AdminCombos = new();
        List<ComboProductTemp> ComboBuilder = new();

        bool _loading = true;
        bool ShowCreateCombo;
        byte[]? _pendingImageBytes;
        string? _pendingImageName;

        IBrowserFile? _selectedFile;
        string? _selectedFileName;

        ComboCreateModel NewComboModel = new();

        protected override async Task OnInitializedAsync()
        {
            _products = await ProductService.GetProductDetailsAsync();
            await LoadCombos();
        }

        async Task LoadCombos()
        {
            _loading = true;
            AdminCombos = await ComboService.GetAdminCombosAsync();
            _loading = false;
        }

        void ShowCreateSection() => ShowCreateCombo = true;

        async Task OpenQuantityModal(ProductDetailDto product)
        {
            var dialog = DialogService.Show<QuantityDialog>("Cantidad",
                new DialogParameters { ["SelectedProduct"] = product });

            var result = await dialog.Result;

            if (!result.Canceled && result.Data is int qty)
                AddProduct(product, qty);
        }

        void AddProduct(ProductDetailDto product, int qty)
        {
            var item = ComboBuilder.FirstOrDefault(x => x.ProductId == product.ProductId);

            if (item != null)
                item.Quantity += qty;
            else
                ComboBuilder.Add(new ComboProductTemp
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    Quantity = qty,
                    ImageUrl = product.ImageUrl ?? ""
                });
        }

        async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;

                // Copia inmediata a memoria local (rompe dependencia de Blazor internals)
                using var stream = file.OpenReadStream(10 * 1024 * 1024);
                using var ms = new MemoryStream();

                await stream.CopyToAsync(ms);
                var bytes = ms.ToArray();

                _selectedFileName = file.Name;

                // guardas bytes en memoria, no el IBrowserFile
                _pendingImageBytes = bytes;
                _pendingImageName = file.Name;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        void ClearSelectedImage()
        {
            _selectedFile = null;
            _selectedFileName = null;
        }

        async Task SaveCombo()
        {
            try
            {
                if (_pendingImageBytes != null)
                    NewComboModel.ImageUrl = await SaveImage();

                NewComboModel.Products = ComboBuilder;

                await ComboService.CreateComboAsync(NewComboModel);

                Snackbar.Add("Combo creado correctamente", Severity.Success);

                ShowCreateCombo = false;
                ComboBuilder.Clear();
                ClearSelectedImage();
                NewComboModel = new();

                await LoadCombos();
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
        }

        async Task<string> SaveImage()
        {
            try
            {
                var folder = Path.Combine(Env.WebRootPath, "images", "Combos");
                Directory.CreateDirectory(folder);

                var ext = Path.GetExtension(_pendingImageName ?? ".jpg");
                var fileName = $"{Guid.NewGuid()}{ext}";
                var path = Path.Combine(folder, fileName);

                await File.WriteAllBytesAsync(path, _pendingImageBytes!);

                return $"/images/Combos/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR GUARDANDO IMAGEN: " + ex.Message);
                throw;
            }
        }

        void ViewCombo(int id) { }
        void EditCombo(int id) { }
        void DeleteCombo(int id) { }
    }
}