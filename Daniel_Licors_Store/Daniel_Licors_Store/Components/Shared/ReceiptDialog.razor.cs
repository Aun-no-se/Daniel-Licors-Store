using System.Globalization;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Cart;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Shared;

public partial class ReceiptDialog
{
    [Parameter]
    public ReceiptDto? Receipt { get; set; }

    /// <summary>
    /// Lo que pasa al confirmar: la página del carrito hace la compra, manda el correo
    /// y devuelve el resultado, que este recibo muestra.
    /// </summary>
    [Parameter]
    public Func<ReceiptOptionsDto, Task<PurchaseOutcomeDto>>? ConfirmPurchase { get; set; }

    [Inject] private IWhatsAppService WhatsAppService { get; set; } = default!;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = default!;

    private bool _sendWhatsApp;
    private string _phone = string.Empty;
    private string? _phoneError;
    private string? _error;
    private bool _processing;
    private bool _purchased;
    private PurchaseOutcomeDto _outcome = new();

    private static string Money(decimal value) =>
        "RD$" + value.ToString("N2", CultureInfo.InvariantCulture);

    private async Task ConfirmAsync()
    {
        if (_processing || _purchased || Receipt == null || ConfirmPurchase == null)
            return;

        _error = null;
        _phoneError = null;

        if (_sendWhatsApp && !WhatsAppService.TryNormalizePhone(_phone, out _))
        {
            _phoneError = "Escribe un número válido, por ejemplo 809 555 1234.";
            return;
        }

        _processing = true;
        StateHasChanged();

        try
        {
            var outcome = await ConfirmPurchase(new ReceiptOptionsDto
            {
                SendWhatsApp = _sendWhatsApp,
                Phone = _phone
            });

            if (outcome.Success)
            {
                _outcome = outcome;
                _purchased = true;
            }
            else
            {
                _error = string.IsNullOrWhiteSpace(outcome.Message)
                    ? "No se pudo completar la compra. Revisa tu carrito e intenta de nuevo."
                    : outcome.Message;
            }
        }
        catch (Exception)
        {
            _error = "No se pudo completar la compra. No se descontó stock ni se creó ningún pedido. Intenta de nuevo.";
        }
        finally
        {
            _processing = false;
        }
    }

    // Comprada → Ok (el carrito se recarga). Sin comprar → Cancel.
    private void CloseDialog()
    {
        if (_processing)
            return;

        if (_purchased)
            MudDialog.Close(DialogResult.Ok(true));
        else
            MudDialog.Cancel();
    }
}
