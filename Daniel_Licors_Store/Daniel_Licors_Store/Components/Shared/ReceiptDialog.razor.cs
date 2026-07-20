using Daniel_Licors_Store.Models.Cart;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Shared;

public partial class ReceiptDialog
{
    [Parameter]
    public ReceiptDto? Receipt { get; set; }

    [Parameter]
    public string WhatsAppLink { get; set; } = string.Empty;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    // Cerrar sin confirmar → result.Canceled = true
    private void CloseDialog()
    {
        MudDialog.Cancel();
    }

    // Confirmar compra → result.Canceled = false, result.Data = true
    private void ConfirmAndClose()
    {
        MudDialog.Close(DialogResult.Ok(true));
    }
}
