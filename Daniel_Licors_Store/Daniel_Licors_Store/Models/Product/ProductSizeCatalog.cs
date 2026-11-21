using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Daniel_Licors_Store.Models.Product;

/// <summary>Un tamaño que el empleado puede elegir con un botón en "Agregar bebida".</summary>
/// <param name="Label">Texto que se guarda y se muestra (ej. "750 ml", "1 L").</param>
/// <param name="Ml">Volumen en mililitros, solo para ordenar de menor a mayor (null si no aplica).</param>
/// <param name="Hint">Texto pequeño bajo el botón (ej. "12 oz", "Media botella").</param>
public sealed record SizeOption(string Label, int? Ml, string? Hint = null);

/// <summary>Tamaños disponibles para un tipo de bebida y si el tamaño es obligatorio.</summary>
public sealed class SizeRule
{
    public string Family { get; init; } = "otros";
    public bool Required { get; init; }
    public IReadOnlyList<SizeOption> Options { get; init; } = Array.Empty<SizeOption>();

    public bool Contains(string? label)
        => !string.IsNullOrWhiteSpace(label)
           && Options.Any(o => string.Equals(o.Label, label.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Catálogo de tamaños (presentaciones) por tipo de bebida.
///
/// La categoría de la bebida decide qué botones aparecen. Se reconoce por palabras del
/// nombre de la categoría (ej. "Ron", "Whisky", "Cervezas", "Vino Tinto", "Energizantes"),
/// así que si el dueño crea una categoría nueva (ej. "Ginebra") también recibe sus tamaños.
/// Si una categoría no se reconoce, se ofrecen tamaños genéricos y el tamaño es opcional.
/// Para cualquier tamaño que no esté en la lista existe el botón "Otro…".
/// </summary>
public static class ProductSizeCatalog
{
    public const int MaxLabelLength = 40;

    // ── Destilados: ron, whisky, vodka, tequila, ginebra, brandy, licores dulces… ──
    private static readonly SizeRule Spirits = new()
    {
        Family = "destilados",
        Required = true,
        Options = new SizeOption[]
        {
            new("50 ml", 50, "Miniatura"),
            new("200 ml", 200),
            new("375 ml", 375, "Media botella"),
            new("750 ml", 750, "Botella"),
            new("1 L", 1000, "Litro"),
            new("1.75 L", 1750),
        }
    };

    // ── Cerveza (en RD: 7 oz, 12 oz, 22 oz y litro; Corona y otras: 710 / 940 ml) ──
    private static readonly SizeRule Beer = new()
    {
        Family = "cerveza",
        Required = true,
        Options = new SizeOption[]
        {
            new("207 ml", 207, "7 oz"),
            new("355 ml", 355, "12 oz"),
            new("473 ml", 473, "16 oz"),
            new("650 ml", 650, "22 oz"),
            new("710 ml", 710, "24 oz"),
            new("940 ml", 940),
            new("1 L", 1000, "Litro"),
        }
    };

    // ── Vino, champagne y espumantes ──
    private static readonly SizeRule Wine = new()
    {
        Family = "vino",
        Required = true,
        Options = new SizeOption[]
        {
            new("187 ml", 187, "Individual"),
            new("375 ml", 375, "Media botella"),
            new("750 ml", 750, "Botella"),
            new("1.5 L", 1500, "Magnum"),
            new("3 L", 3000),
        }
    };

    // ── Energizantes (Red Bull: 250, 355, 473 y 591 ml) ──
    private static readonly SizeRule Energy = new()
    {
        Family = "energizante",
        Required = true,
        Options = new SizeOption[]
        {
            new("250 ml", 250, "8.4 oz"),
            new("355 ml", 355, "12 oz"),
            new("473 ml", 473, "16 oz"),
            new("591 ml", 591, "20 oz"),
        }
    };

    // ── Mixers, refrescos, sodas, aguas, jugos ──
    private static readonly SizeRule Soft = new()
    {
        Family = "mixers",
        Required = true,
        Options = new SizeOption[]
        {
            new("200 ml", 200),
            new("355 ml", 355, "12 oz"),
            new("500 ml", 500),
            new("1 L", 1000, "Litro"),
            new("1.5 L", 1500),
            new("2 L", 2000),
            new("3 L", 3000),
        }
    };

    // ── Snacks (se venden por tamaño de funda, no por volumen) ──
    private static readonly SizeRule Snacks = new()
    {
        Family = "snacks",
        Required = false,
        Options = new SizeOption[]
        {
            new("Pequeño", null),
            new("Mediano", null),
            new("Grande", null),
            new("Familiar", null),
        }
    };

    // ── Categoría que no se reconoce: opciones genéricas y tamaño opcional ──
    private static readonly SizeRule Other = new()
    {
        Family = "otros",
        Required = false,
        Options = new SizeOption[]
        {
            new("Pequeño", null),
            new("Mediano", null),
            new("Grande", null),
            new("Familiar", null),
        }
    };

    // ════════════════════════════════════════════════════════════════
    //  API
    // ════════════════════════════════════════════════════════════════

    /// <summary>Devuelve los tamaños que corresponden a una categoría (por su nombre).</summary>
    public static SizeRule GetRule(string? categoryName)
    {
        var tokens = Tokens(categoryName);
        if (tokens.Count == 0) return Other;

        if (Has(tokens, prefixes: new[] { "energiz", "energy", "energet" })) return Energy;
        if (Has(tokens, prefixes: new[] { "cerveza", "beer", "birra" })) return Beer;
        if (Has(tokens, prefixes: new[] { "champ", "espum", "cava", "prosecco", "vino", "wine", "sidra" })) return Wine;

        if (Has(tokens,
                exact: new[] { "ron", "gin", "rum" },
                prefixes: new[]
                {
                    "whisk", "vodka", "tequila", "ginebra", "brandy", "cognac", "conac", "licor",
                    "mezcal", "mamajuana", "aguardiente", "vermu", "amaretto", "pisco", "cacha",
                    "bourbon", "destil", "espiritu"
                }))
            return Spirits;

        if (Has(tokens,
                exact: new[] { "agua", "aguas", "soda", "sodas" },
                prefixes: new[] { "mixer", "refresc", "gaseos", "jugo", "malta", "tonic", "gingerale" }))
            return Soft;

        if (Has(tokens, prefixes: new[] { "snack", "pasapalo", "picadera", "galleta", "papita" })) return Snacks;

        return Other;
    }

    /// <summary>Limpia el texto de un tamaño (espacios, largo). Devuelve null si queda vacío.</summary>
    public static string? NormalizeLabel(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var clean = Regex.Replace(raw.Trim(), @"\s+", " ");
        if (clean.Length > MaxLabelLength) clean = clean[..MaxLabelLength].TrimEnd();

        return clean.Length == 0 ? null : clean;
    }

    private static readonly Regex VolumeRegex = new(
        @"^(\d+(?:[.,]\d+)?)\s*(ml|cl|l|lt|lts|litro|litros|oz|onza|onzas)\.?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Intenta convertir un texto como "355 ml", "1.14 L" o "12 oz" a mililitros.
    /// Devuelve null si el texto no es un volumen (ej. "Familiar").
    /// </summary>
    public static int? TryParseMl(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return null;

        var m = VolumeRegex.Match(label.Trim());
        if (!m.Success) return null;

        if (!double.TryParse(m.Groups[1].Value.Replace(',', '.'),
                NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            return null;

        var factor = m.Groups[2].Value.ToLowerInvariant() switch
        {
            "ml" => 1d,
            "cl" => 10d,
            "oz" or "onza" or "onzas" => 29.5735d,
            _ => 1000d // l, lt, lts, litro(s)
        };

        var ml = (int)Math.Round(value * factor);
        return ml > 0 ? ml : null;
    }

    // ── Internos ─────────────────────────────────────────────────────
    private static bool Has(List<string> tokens, string[]? exact = null, string[]? prefixes = null)
    {
        foreach (var t in tokens)
        {
            if (exact != null && exact.Contains(t)) return true;
            if (prefixes != null && prefixes.Any(p => t.StartsWith(p, StringComparison.Ordinal))) return true;
        }
        return false;
    }

    private static List<string> Tokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        return Regex.Split(normalized, @"[^a-z0-9]+")
                    .Where(t => t.Length > 0)
                    .ToList();
    }

    private static string RemoveDiacritics(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

/// <summary>
/// Arma el nombre completo de una bebida con su tamaño: "Corona (355 ml)".
/// Se usa en el carrito, combos, promociones, stock y en el mensaje de WhatsApp.
/// </summary>
public static class ProductNaming
{
    public static string Compose(string? name, string? size)
    {
        var n = (name ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(size) ? n : $"{n} ({size.Trim()})";
    }
}
