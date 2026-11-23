using System.Globalization;
using System.Text;
using Daniel_Licors_Store.Models.Cart;

namespace Daniel_Licors_Store.Data.Services;

public interface IWhatsAppService
{
    string GenerateReceiptMessage(ReceiptDto receipt);
    Task<bool> SendReceiptAsync(string phoneNumber, ReceiptDto receipt);
    string GenerateWhatsAppLink(string phoneNumber, string message);

    /// <summary>
    /// Valida y normaliza el número del cliente. Acepta "809 555 1234", "(829) 555-1234",
    /// "+1 849 555 1234" o un número internacional con "+". Devuelve solo dígitos con código de país.
    /// </summary>
    bool TryNormalizePhone(string? input, out string digits);

    /// <summary>Link de WhatsApp al número del cliente con su recibo ya escrito.</summary>
    string GenerateCustomerReceiptLink(string normalizedDigits, ReceiptDto receipt);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(ILogger<WhatsAppService> logger)
    {
        _logger = logger;
    }

    private static string Money(decimal value) =>
        "RD$" + value.ToString("N2", CultureInfo.InvariantCulture);

    // Mensaje limpio, pensado para leerse en el celular:
    //   • Corona Extra (355 ml)
    //     3 × RD$50.00 = *RD$150.00*
    public string GenerateReceiptMessage(ReceiptDto receipt)
    {
        var sb = new StringBuilder();

        sb.AppendLine("🧾 *RECIBO DE COMPRA*");
        sb.AppendLine("*Daniel Liquor Store*");
        if (!string.IsNullOrWhiteSpace(receipt.StoreLocation))
            sb.AppendLine(receipt.StoreLocation);
        sb.AppendLine();
        sb.AppendLine($"Recibo: {receipt.ReceiptNumber}");
        sb.AppendLine($"Fecha: {receipt.CreatedAt.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("*TU COMPRA*");

        foreach (var item in receipt.Items)
        {
            sb.AppendLine($"• {item.ItemName}");
            sb.AppendLine($"   {item.Quantity} × {Money(item.UnitPrice)} = *{Money(item.SubTotal)}*");
        }

        sb.AppendLine();
        sb.AppendLine($"*TOTAL ({receipt.TotalUnits} artículo{(receipt.TotalUnits == 1 ? "" : "s")}): {Money(receipt.TotalAmount)}*");
        sb.AppendLine();
        sb.AppendLine("¡Gracias por tu compra! 🍷");

        return sb.ToString();
    }

    public bool TryNormalizePhone(string? input, out string digits)
    {
        digits = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var raw = input.Trim();
        var international = raw.StartsWith("+");
        var onlyDigits = new string(raw.Where(char.IsDigit).ToArray());

        if (onlyDigits.Length < 10 || onlyDigits.Length > 15)
            return false;

        // 10 dígitos sin "+" = número de República Dominicana / EE. UU. (809, 829, 849...)
        if (!international && onlyDigits.Length == 10)
            onlyDigits = "1" + onlyDigits;

        digits = onlyDigits;
        return true;
    }

    public string GenerateCustomerReceiptLink(string normalizedDigits, ReceiptDto receipt) =>
        GenerateWhatsAppLink(normalizedDigits, GenerateReceiptMessage(receipt));

    public Task<bool> SendReceiptAsync(string phoneNumber, ReceiptDto receipt)
    {
        try
        {
            var link = GenerateWhatsAppLink(phoneNumber, GenerateReceiptMessage(receipt));
            _logger.LogInformation("Link de WhatsApp generado para {Phone}: {Link}", phoneNumber, link);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando el recibo para WhatsApp");
            return Task.FromResult(false);
        }
    }

    public string GenerateWhatsAppLink(string phoneNumber, string message)
    {
        // wa.me exige solo dígitos (sin "+", espacios ni guiones).
        var clean = new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        return $"https://wa.me/{clean}?text={Uri.EscapeDataString(message)}";
    }
}
