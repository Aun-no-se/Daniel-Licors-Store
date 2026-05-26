using Daniel_Licors_Store.Components.Dialogs;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services.Interfaces;
using Daniel_Licors_Store.Models.Combo;
using Daniel_Licors_Store.Models.Product;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages
{
    public partial class Combos
    {
        [Inject] public IComboService ComboService { get; set; } = default!;
        [Inject] public ISnackbar Snackbar { get; set; } = default!;
        [Inject] public IProductService ProductService { get; set; } = default!;

        [Inject] public IDialogService DialogService { get; set; } = default!;

        ProductDetailDto? SelectedProduct;
        int SelectedQuantity = 1;

        List<ProductDetailDto> _products = new();

        List<ComboAdminDto> AdminCombos = new();
        bool _loading = true;

        bool ShowCreateCombo = false;

        // productos seleccionados para el combo (en memoria)
        List<ComboProductTemp> ComboBuilder = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCombos();
            _products = await ProductService.GetProductDetailsAsync();
        }

        private async Task LoadCombos()
        {
            _loading = true;

            try
            {
                AdminCombos = await ComboService.GetAdminCombosAsync();
            }
            catch
            {
                Snackbar.Add("Error cargando los combos", Severity.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        private void ShowCreateSection()
        {
            ShowCreateCombo = true;
        }

        private void ViewCombo(int comboId)
        {
            Snackbar.Add($"Ver combo {comboId}", Severity.Normal);
        }

        private void EditCombo(int comboId)
        {
            Snackbar.Add($"Editar combo {comboId}", Severity.Warning);
        }

        private void DeleteCombo(int comboId)
        {
            Snackbar.Add($"Eliminar combo {comboId}", Severity.Error);
        }

        // CAMBIO: Añadimos async para poder usar await de forma limpia
        async Task OpenQuantityModal(ProductDetailDto product)
        {
            SelectedProduct = product;
            SelectedQuantity = 1;

            var parameters = new DialogParameters
    {
        { "SelectedProduct", product }
    };

            // 1. Abrimos el modal y guardamos la referencia
            var dialog = DialogService.Show<QuantityDialog>(
                "Cantidad",
                parameters,
                new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall }
            );

            // 2. Esperamos de verdad el resultado usando await de forma limpia
            var result = await dialog.Result;

            // 3. Evaluamos si no se canceló
            if (result != null && !result.Canceled)
            {
                // En MudBlazor el Data viene directamente tipado o como object
                if (result.Data is int quantity)
                {
                    AddProductToCombo(product, quantity);
                    StateHasChanged(); // Forzamos el renderizado de la lista en la pantalla principal
                }
            }
        }

        void AddProductToCombo(ProductDetailDto product, int quantity)
        {
            var existing = ComboBuilder.FirstOrDefault(p => p.ProductId == product.ProductId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                ComboBuilder.Add(new ComboProductTemp
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl ?? "",
                    Quantity = quantity
                });
            }
        }
    }
}