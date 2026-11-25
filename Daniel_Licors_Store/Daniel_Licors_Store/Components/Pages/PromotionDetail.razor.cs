using System.Globalization;
using System.Security.Claims;
using Daniel_Licors_Store.Components.Shared;
using Daniel_Licors_Store.Data.Repositories.Interfaces;
using Daniel_Licors_Store.Data.Services;
using Daniel_Licors_Store.Models.Promotion;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Pages;

public partial class PromotionDetail
{
    [Parameter] public int PromotionId { get; set; }

    [Inject] private IPromotionService PromotionService { get; set; } = default!;
    [Inject] private ICartService CartService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private static readonly CultureInfo _es = new("es-DO");

    private PromotionDetailDto? Promo;
    private bool _loading = true;
    private bool _adding;
    private bool _isStaff;

    // ── Carga ────────────────────────────────────────────────────────────
    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        Promo = null;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            _isStaff = auth.User.IsInRole("Admin") || auth.User.IsInRole("Empleado");

            var promo = await PromotionService.GetPromotionDetailAsync(PromotionId);

            // Una promoción desactivada solo la ve el personal; para un cliente "no existe".
            Promo = promo != null && (promo.IsActive || _isStaff) ? promo : null;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error cargando la promoción: {ex.Message}", Severity.Error);
        }
        finally
        {
            _loading = false;
        }
    }

    // ── Lo que muestra la vista ──────────────────────────────────────────
    private string? PromoImage =>
        !string.IsNullOrWhiteSpace(Promo?.ImageUrl)
            ? Promo!.ImageUrl
            : Promo?.Lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l.ImageUrl))?.ImageUrl;

    private string StatusClass => Promo?.StatusLabel switch
    {
        "Vigente" => "pm-status-live",
        "Programada" => "pm-status-upcoming",
        "Vencida" => "pm-status-expired",
        _ => "pm-status-off"
    };

    private static string Fmt(DateTime d) => d.ToString("dd MMM yyyy", _es);

    private string TimeText
    {
        get
        {
            if (Promo == null) return string.Empty;
            if (!Promo.IsActive) return "Desactivada";

            var now = DateTime.Now;

            if (Promo.StartDate > now)
            {
                var d = (int)Math.Ceiling((Promo.StartDate - now).TotalDays);
                return d <= 1 ? "Empieza mañana" : $"Empieza en {d} días";
            }

            // La fecha fin cuenta completa: la promoción vale hasta el final de ese día.
            var daysSinceEnd = (now.Date - Promo.EndDate.Date).Days;
            if (daysSinceEnd > 0)
                return daysSinceEnd == 1 ? "Terminó ayer" : $"Terminó hace {daysSinceEnd} días";

            var daysLeft = (Promo.EndDate.Date - now.Date).Days;
            if (daysLeft == 0) return "Termina hoy";
            return daysLeft == 1 ? "Termina mañana" : $"Faltan {daysLeft} días";
        }
    }

    // Ahorro total según cómo está definida la promoción.
    private string? TotalSavingText
    {
        get
        {
            if (Promo == null || Promo.Lines.Count == 0) return null;

            if (Promo.PromotionType == "PayXGetY")
            {
                var pack = Promo.Lines.Where(l => l.IsPayXGetY).Sum(l => l.PackSaving);
                return pack > 0 ? $"Ahorras ${pack.ToString("N2", CultureInfo.InvariantCulture)} por paquete" : null;
            }

            if (Promo.PromotionType == "Bundle")
            {
                var pack = Promo.Lines.Sum(l => l.Quantity * l.SavingPerUnit);
                return pack > 0 ? $"Ahorras ${pack.ToString("N2", CultureInfo.InvariantCulture)} por paquete" : null;
            }

            var total = Promo.Lines.Sum(l => l.SavingPerUnit);
            if (total <= 0) return null;

            return Promo.Lines.Count == 1
                ? $"Ahorras ${total.ToString("N2", CultureInfo.InvariantCulture)} por unidad"
                : $"Ahorras ${total.ToString("N2", CultureInfo.InvariantCulture)} llevando 1 de cada bebida";
        }
    }

    private static string CardBadge(PromotionDetailLine line)
    {
        if (line.IsFree) return "GRATIS";

        if (line.IsPayXGetY && !line.SpecialPrice.HasValue && !line.DiscountPercentage.HasValue)
            return $"Lleva {line.RequiredQuantity} · Paga {line.PayQuantity}";

        return line.SavingPercent > 0 ? $"-{line.SavingPercent:0.##}%" : "Promo";
    }

    // ── Navegación ───────────────────────────────────────────────────────
    private void GoBack() => Nav.NavigateTo(_isStaff ? "/admin/promociones" : "/");

    private void GoToProduct(int productId) => Nav.NavigateTo($"/producto/{productId}");

    // ── Agregar al carrito (misma regla que en el detalle del producto) ──
    private async Task AddPromotionToCart()
    {
        if (Promo == null || _adding) return;
        _adding = true;

        try
        {
            var auth = await AuthStateProvider.GetAuthenticationStateAsync();
            var userId = auth.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                Nav.NavigateTo("/login");
                return;
            }

            // Una promoción es un trato puntual: si ya está en el carrito, se avisa.
            var carrito = await CartService.GetCartAsync(userId, AppStore.StoreId);
            if (carrito.Promotions.Any(p => p.PromotionId == Promo.PromotionId))
            {
                Snackbar.Add($"Ya tienes la promoción «{Promo.Title}» en tu carrito", Severity.Info);
                return;
            }

            var payLine = Promo.Lines.FirstOrDefault(l => l.IsPayXGetY);
            var qty = payLine?.RequiredQuantity ?? 1;

            await CartService.AddPromotionToCartAsync(userId, Promo.PromotionId, qty, AppStore.StoreId);
            Snackbar.Add($"Promoción «{Promo.Title}» agregada al carrito", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            _adding = false;
        }
    }
}
