using Daniel_Licors_Store.Models.Cart;

namespace Daniel_Licors_Store.Data.Services;

public interface IWhatsAppService
{
    string GenerateReceiptMessage(ReceiptDto receipt);
    Task<bool> SendReceiptAsync(string phoneNumber, ReceiptDto receipt);
    string GenerateWhatsAppLink(string phoneNumber, string message);
}

public class WhatsAppService : IWhatsAppService
{
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IConfiguration _configuration;

    public WhatsAppService(ILogger<WhatsAppService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public string GenerateReceiptMessage(ReceiptDto receipt)
    {
        var message = new System.Text.StringBuilder();

        message.AppendLine("🛍️ *RECIBO DE COMPRA*");
        message.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine($"*Nro. Recibo:* {receipt.ReceiptNumber}");
        message.AppendLine($"*Cliente:* {receipt.CustomerName}");
        message.AppendLine($"*Tienda:* {receipt.StoreLocation}");
        message.AppendLine($"*Fecha:* {receipt.CreatedAt:dd/MM/yyyy HH:mm}");
        message.AppendLine($"");
        message.AppendLine($"*DETALLE DE COMPRA*");
        message.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");

        var productCount = 0;
        var comboCount = 0;
        var promotionCount = 0;

        foreach (var item in receipt.Items)
        {
            var formattedItem = $"• {item.ItemName}";
            formattedItem += $"\n  Cantidad: {item.Quantity}";
            formattedItem += $"\n  Precio: ${item.UnitPrice:N2} x {item.Quantity}";
            formattedItem += $"\n  Subtotal: ${item.SubTotal:N2}\n";

            message.AppendLine(formattedItem);

            if (item.Category == "Producto") productCount++;
            else if (item.Category == "Combo") comboCount++;
            else if (item.Category == "Promoción") promotionCount++;
        }

        message.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine($"*TOTAL A PAGAR: ${receipt.TotalAmount:N2}*");
        message.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        message.AppendLine($"");
        message.AppendLine($"📊 *Resumen:*");
        if (productCount > 0) message.AppendLine($"  • Productos: {productCount}");
        if (comboCount > 0) message.AppendLine($"  • Combos: {comboCount}");
        if (promotionCount > 0) message.AppendLine($"  • Promociones: {promotionCount}");
        message.AppendLine($"");
        message.AppendLine($"✅ *Compra completada exitosamente*");
        message.AppendLine($"");
        message.AppendLine($"¡Gracias por tu compra en Daniel Licors Store! 🍷");

        return message.ToString();
    }

    public Task<bool> SendReceiptAsync(string phoneNumber, ReceiptDto receipt)
    {
        try
        {
            var message = GenerateReceiptMessage(receipt);
            var whatsappLink = GenerateWhatsAppLink(phoneNumber, message);
            
            _logger.LogInformation($"WhatsApp link generado para {phoneNumber}");
            _logger.LogInformation($"Link: {whatsappLink}");

            // En una implementación real, aquí usarías una API de WhatsApp
            // Por ahora, solo generamos el link para que el usuario pueda hacer clic

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp receipt");
            return Task.FromResult(false);
        }
    }

    public string GenerateWhatsAppLink(string phoneNumber, string message)
    {
        // Formato internacional: +1234567890 (sin caracteres especiales)
        var cleanPhone = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");
        
        // Si no tiene +, agregarlo
        if (!cleanPhone.StartsWith("+"))
            cleanPhone = "+" + cleanPhone;

        // Codificar mensaje para URL
        var encodedMessage = System.Uri.EscapeDataString(message);

        // Retornar link directo de WhatsApp
        return $"https://wa.me/{cleanPhone}?text={encodedMessage}";
    }
}
