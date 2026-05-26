using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Dialogs
{
    public partial class QuantityDialog
    {
        [CascadingParameter]
        public IMudDialogInstance MudDialog { get; set; } = default!;

        // CAMBIO AQUÍ: Cambiamos el nombre a SelectedProduct para romper el conflicto con la entidad Product
        [Parameter]
        public Daniel_Licors_Store.Models.Product.ProductDetailDto SelectedProduct { get; set; } = default!;

        public int Quantity { get; set; } = 1;

        public void Cancel()
        {
            MudDialog.Cancel();
        }

        public void Confirm()
        {
            MudDialog.Close(DialogResult.Ok(Quantity));
        }
    }
}