using System.Globalization;
using System.Text;
using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using MudBlazor;

namespace Daniel_Licors_Store.Components.Shared
{
    /// <summary>
    /// Barra infinita (marquee) con las categorías activas (una por sección del index).
    /// Se llena sola desde la base de datos: lo que el admin agrega en
    /// "Categorías y Marcas" aparece aquí. Las marcas no tienen sección, así que no van. El movimiento, el arrastre y el
    /// clic-para-ir-a-la-sección viven en wwwroot/js/category-marquee.js;
    /// Blazor solo pinta el HTML (nada de JS interop, nada de nodos
    /// insertados por JS dentro del DOM de Blazor).
    /// </summary>
    public partial class CategoryMarquee : IDisposable
    {
        // Contexto por operación (igual que CatalogService) para no chocar
        // con el DbContext scoped que usan otros componentes de la página.
        [Inject] private IDbContextFactory<ApplicationDbContext> Factory { get; set; } = default!;
        [Inject] private ProductStateService State { get; set; } = default!;

        // Kind: "categoria" | "marca" | "sep"
        private sealed record MarqueeItem(
            string Kind,
            string Label,
            string Name,
            string Target,
            string Glyph,
            string Accent);

        private List<MarqueeItem> _items = new();
        private int _repeat = 1;
        private bool _loading = true;
        private bool _disposed;

        protected override async Task OnInitializedAsync()
        {
            State.OnChange += OnStateChanged;
            await LoadAsync();
            _loading = false;
        }

        private void OnStateChanged()
        {
            if (_disposed) return;
            InvokeAsync(async () =>
            {
                if (_disposed) return;
                await LoadAsync();
                StateHasChanged();
            });
        }

        private async Task LoadAsync()
        {
            try
            {
                await using var ctx = await Factory.CreateDbContextAsync();

                var categories = await ctx.Categories.AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.CategoryId, c.Name })
                    .ToListAsync();

                var items = new List<MarqueeItem>();

                foreach (var c in categories)
                {
                    var (glyph, accent) = ResolveVisual(c.Name);
                    items.Add(new MarqueeItem("categoria", "Categoría", c.Name,
                        $"categoria-{c.CategoryId}", glyph, accent));
                }
                if (categories.Count > 0)
                    items.Add(Separator());

                // Cuántas veces repetir el grupo para que SIEMPRE llene una
                // pantalla ancha (si hay pocas categorías/marcas, se repiten).
                var real = items.Count(i => i.Kind != "sep");
                _repeat = real == 0 ? 1 : Math.Clamp((int)Math.Ceiling(3600d / (real * 200d)), 1, 10);
                _items = items;
            }
            catch
            {
                // Si la consulta falla, la barra simplemente no se muestra;
                // no debe tumbar el index.
                _items = new List<MarqueeItem>();
            }
        }

        private static MarqueeItem Separator() => new("sep", "", "", "", "", "");

        // ── Ícono + color por categoría ─────────────────────────────────────
        // Cada categoría tiene su propio "logo". Vodka y ron comparten
        // botella (licor fuerte) pero con color distinto; la cerveza lleva
        // vaso con espuma, el champagne copa alta con burbujas, etc.
        private static (string Glyph, string Accent) ResolveVisual(string? name)
        {
            var n = Normalize(name);
            var tokens = n.Split(new[] { ' ', '-', '/', '&', ',', '.', '(', ')' },
                                 StringSplitOptions.RemoveEmptyEntries);

            bool Has(params string[] keys) => keys.Any(k => n.Contains(k));
            bool Tok(params string[] keys) => tokens.Any(t => keys.Contains(t));

            if (Has("cerveza", "beer", "malta"))
                return (Icons.Material.Filled.SportsBar, "#F2B33D");

            if (Has("champa", "espumante", "prosecco") || Tok("cava", "brut"))
                return (GlyphChampagne, "#F5DFA0");

            if (Has("vino", "wine"))
            {
                if (Has("blanco")) return (Icons.Material.Filled.WineBar, "#EAD98B");
                if (Has("rosado", "rose")) return (Icons.Material.Filled.WineBar, "#F29AB0");
                return (Icons.Material.Filled.WineBar, "#E5354F");
            }

            if (Tok("ron", "rones", "rum"))
                return (Icons.Material.Filled.Liquor, "#D08A4A");

            if (Has("vodka"))
                return (Icons.Material.Filled.Liquor, "#BFD4E6");

            if (Has("ginebra") || Tok("gin"))
                return (Icons.Material.Filled.Liquor, "#7ED6C0");

            if (Has("whisk", "scotch", "bourbon") || Has("brandy", "cognac"))
                return (GlyphRocks, "#D9A05B");

            if (Has("tequila", "mezcal"))
                return (GlyphShot, "#9CCB5E");

            if (Has("energi"))
                return (Icons.Material.Filled.Bolt, "#FFD83D");

            if (Has("mixer", "refresco", "soda", "jugo", "agua"))
                return (Icons.Material.Filled.LocalDrink, "#7FC8E8");

            if (Has("snack", "picadera"))
                return (Icons.Material.Filled.Fastfood, "#E8935A");

            if (Has("dulce"))
                return (Icons.Material.Filled.Cake, "#F08FB2");

            if (Has("licor"))
                return (Icons.Material.Filled.Liquor, "#E8C468");

            return (Icons.Material.Filled.LocalBar, "#E8C468");
        }

        private static string Normalize(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var formD = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // ── Glifos propios (24x24, relleno sólido, mismo estilo Material) ───
        // Copa de champagne alta con burbujas y destello
        private const string GlyphChampagne =
            "<path fill-rule=\"evenodd\" d=\"M8.8 2h6.4l-.55 8.1c-.15 2.3-1.1 3.9-2.65 4.5-1.55-.6-2.5-2.2-2.65-4.5L8.8 2z" +
            "M10.45 6.2a.65.65 0 1 1 1.3 0 .65.65 0 1 1-1.3 0z" +
            "M12.25 8.5a.55.55 0 1 1 1.1 0 .55.55 0 1 1-1.1 0z" +
            "M10.75 10.7a.5.5 0 1 1 1 0 .5.5 0 1 1-1 0z" +
            "M11.1 14.6h1.8V20H16v2H8v-2h3.1v-5.4z" +
            "M18.5 2.4l.7 1.6 1.6.7-1.6.7-.7 1.6-.7-1.6-1.6-.7 1.6-.7z\"/>";

        // Vaso corto con hielo (whisky / brandy)
        private const string GlyphRocks =
            "<path fill-rule=\"evenodd\" d=\"M4.8 3h14.4l-1.55 16.3A2 2 0 0 1 15.66 21H8.34a2 2 0 0 1-1.99-1.7L4.8 3z" +
            "M6.35 4.6h11.3l-.35 3.7H6.7z" +
            "M8.6 10.4h3v3h-3z" +
            "M12.4 12.6h3v3h-3z\"/>";

        // Vasito de shot con rodaja de limón (tequila / mezcal)
        private const string GlyphShot =
            "<path fill-rule=\"evenodd\" d=\"M5.2 9.2h9.6l-.95 10.1A2 2 0 0 1 11.86 21H8.14a2 2 0 0 1-1.99-1.7L5.2 9.2z" +
            "M6.6 10.7h6.8l-.25 2.6H6.85z" +
            "M12.4 9.2a3.9 3.9 0 0 1 7.8 0z" +
            "M14.2 9.2a2.1 2.1 0 0 1 4.2 0z" +
            "M16 7.1h.6v2.1H16z\"/>";

        public void Dispose()
        {
            _disposed = true;
            State.OnChange -= OnStateChanged;
        }
    }
}