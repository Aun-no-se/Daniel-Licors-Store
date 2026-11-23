using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Daniel_Licors_Store.Models.Cart;

namespace Daniel_Licors_Store.Data.Services;

public interface IEmailService
{
    /// <summary>True si en appsettings (sección "Email") están los datos del servidor de correo.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Envía el recibo al correo del cliente. Nunca lanza excepciones:
    /// si algo falla devuelve false (la compra ya quedó registrada de todas formas).
    /// </summary>
    Task<bool> SendReceiptAsync(string toEmail, ReceiptDto receipt);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private string Host => _config["Email:Host"] ?? string.Empty;
    private int Port => int.TryParse(_config["Email:Port"], out var p) ? p : 587;
    private bool EnableSsl => !bool.TryParse(_config["Email:EnableSsl"], out var s) || s;
    private string UserName => _config["Email:UserName"] ?? string.Empty;
    private string Password => _config["Email:Password"] ?? string.Empty;
    private string FromAddress => string.IsNullOrWhiteSpace(_config["Email:FromAddress"]) ? UserName : _config["Email:FromAddress"]!;
    private string FromName => string.IsNullOrWhiteSpace(_config["Email:FromName"]) ? "Daniel Liquor Store" : _config["Email:FromName"]!;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Password) &&
        !string.IsNullOrWhiteSpace(FromAddress);

    public async Task<bool> SendReceiptAsync(string toEmail, ReceiptDto receipt)
    {
        if (string.IsNullOrWhiteSpace(toEmail) || receipt == null)
            return false;

        if (!IsConfigured)
        {
            _logger.LogWarning("No se envió el recibo {Receipt}: falta configurar la sección \"Email\" en appsettings.", receipt.ReceiptNumber);
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(FromAddress, FromName, Encoding.UTF8),
                Subject = $"Tu recibo {receipt.ReceiptNumber} · Daniel Liquor Store",
                SubjectEncoding = Encoding.UTF8,
                Body = BuildHtml(receipt),
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(toEmail.Trim()));

            using var client = new SmtpClient(Host, Port)
            {
                EnableSsl = EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(UserName, Password),
                Timeout = 20000
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Recibo {Receipt} enviado a {Email}", receipt.ReceiptNumber, toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo enviar el recibo {Receipt} a {Email}", receipt.ReceiptNumber, toEmail);
            return false;
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    // Plantilla del correo (solo estilos en línea para que se vea igual en Gmail,
    // Outlook y celulares). Cada artículo ocupa dos líneas: nombre + total, y debajo
    // "cantidad × precio", para que se lea bien aunque la pantalla sea angosta.
    // ───────────────────────────────────────────────────────────────────────
    private static string Money(decimal value) =>
        "RD$" + value.ToString("N2", CultureInfo.InvariantCulture);

    private static string H(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static string BuildHtml(ReceiptDto r)
    {
        var sb = new StringBuilder();

        var firstName = (r.CustomerName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        var greeting = string.IsNullOrWhiteSpace(firstName) || firstName.Contains('@')
            ? "¡Gracias por tu compra!"
            : $"¡Gracias por tu compra, {H(firstName)}!";

        sb.Append("<!DOCTYPE html><html lang=\"es\"><head><meta charset=\"utf-8\">")
          .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">")
          .Append("<title>Recibo de compra</title></head>")
          .Append("<body style=\"margin:0;padding:0;background:#120b0b;\">");

        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#120b0b;padding:24px 10px;\"><tr><td align=\"center\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background:#ffffff;border-radius:18px;overflow:hidden;font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:#1f1a1a;\">");

        // Cabecera de marca
        sb.Append("<tr><td align=\"center\" style=\"background:#1a0c0c;padding:26px 20px 22px;border-bottom:3px solid #E8C468;\">")
          .Append("<div style=\"font-size:13px;letter-spacing:3px;color:#E8C468;font-weight:700;\">DANIEL LIQUOR</div>")
          .Append("<div style=\"font-size:11px;letter-spacing:2.5px;color:#b9a77a;margin-top:4px;\">FINE SPIRITS &amp; STORE</div>")
          .Append("</td></tr>");

        // Saludo
        sb.Append("<tr><td style=\"padding:26px 24px 6px;\">")
          .Append($"<div style=\"font-size:22px;font-weight:800;color:#1f1a1a;\">{greeting}</div>")
          .Append("<div style=\"font-size:14px;color:#6b6262;margin-top:6px;line-height:1.5;\">Este es el detalle de tu recibo. Guárdalo como comprobante.</div>")
          .Append("</td></tr>");

        // Datos del recibo (tabla de 2 columnas: se ve bien también en Outlook)
        sb.Append("<tr><td style=\"padding:16px 24px 4px;\">")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#faf6ee;border:1px solid #efe6d2;border-radius:12px;\"><tr><td style=\"padding:10px 16px;\">")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"font-size:13px;\">");
        void MetaRow(string label, string value) =>
            sb.Append($"<tr><td style=\"padding:5px 0;color:#8a7f6e;\">{label}</td><td align=\"right\" style=\"padding:5px 0;font-weight:700;color:#1f1a1a;\">{H(value)}</td></tr>");
        MetaRow("Recibo", r.ReceiptNumber);
        MetaRow("Fecha", r.CreatedAt.ToString("dd/MM/yyyy hh:mm tt", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(r.StoreLocation))
            MetaRow("Sucursal", r.StoreLocation);
        sb.Append("</table></td></tr></table></td></tr>");

        // Artículos
        sb.Append("<tr><td style=\"padding:18px 24px 0;\">")
          .Append("<div style=\"font-size:11px;letter-spacing:1.5px;color:#8a7f6e;font-weight:700;padding-bottom:8px;border-bottom:1px solid #eee4d0;\">TU COMPRA</div>")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\">");

        foreach (var item in r.Items)
        {
            var tag = string.Equals(item.Category, "Producto", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(item.Category)
                ? string.Empty
                : $" <span style=\"background:#f4ead2;color:#7a6320;font-size:10px;font-weight:700;padding:2px 7px;border-radius:20px;\">{H(item.Category)}</span>";

            sb.Append("<tr>")
              .Append("<td style=\"padding:14px 0;border-bottom:1px solid #f1ebdd;vertical-align:top;\">")
              .Append($"<div style=\"font-size:15px;font-weight:700;color:#1f1a1a;line-height:1.3;\">{H(item.ItemName)}</div>")
              .Append($"<div style=\"font-size:13px;color:#6b6262;margin-top:4px;\"><strong style=\"color:#1f1a1a;\">{item.Quantity}</strong> × {Money(item.UnitPrice)}{tag}</div>")
              .Append("</td>")
              .Append($"<td align=\"right\" style=\"padding:14px 0 14px 12px;border-bottom:1px solid #f1ebdd;vertical-align:top;white-space:nowrap;font-size:16px;font-weight:800;color:#1f1a1a;\">{Money(item.SubTotal)}</td>")
              .Append("</tr>");
        }
        sb.Append("</table></td></tr>");

        // Total
        sb.Append("<tr><td style=\"padding:18px 24px 6px;\">")
          .Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:#1a0c0c;border-radius:12px;\"><tr>")
          .Append($"<td style=\"padding:16px 18px;color:#d8c690;font-size:12px;letter-spacing:1.5px;font-weight:700;\">TOTAL · {r.TotalUnits} ARTÍCULO{(r.TotalUnits == 1 ? "" : "S")}</td>")
          .Append($"<td align=\"right\" style=\"padding:16px 18px;color:#E8C468;font-size:24px;font-weight:800;white-space:nowrap;\">{Money(r.TotalAmount)}</td>")
          .Append("</tr></table></td></tr>");

        // Pie
        sb.Append("<tr><td style=\"padding:20px 24px 28px;text-align:center;\">")
          .Append("<div style=\"font-size:14px;color:#1f1a1a;font-weight:700;\">¡Gracias por tu preferencia! 🍷</div>")
          .Append("<div style=\"font-size:12px;color:#8a7f6e;margin-top:6px;line-height:1.5;\">Este recibo se envió al correo con el que creaste tu cuenta.</div>")
          .Append("</td></tr>");

        sb.Append("</table></td></tr></table></body></html>");
        return sb.ToString();
    }
}
