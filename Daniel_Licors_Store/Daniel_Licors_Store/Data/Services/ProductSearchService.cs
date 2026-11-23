using System.Globalization;
using System.Text;
using Daniel_Licors_Store.Models.Product;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services;

public interface IProductSearchService
{
    /// <summary>Carga el índice en segundo plano para que la primera búsqueda sea instantánea.</summary>
    Task WarmUpAsync();

    /// <summary>Busca bebidas. Tolera tildes, mayúsculas, orden de palabras y errores de escritura.</summary>
    Task<List<SearchProductDto>> SearchAsync(string query, ISet<int>? favoriteIds = null, int max = 8);

    /// <summary>Lo que este usuario ya compró, lo más repetido primero.</summary>
    Task<List<SearchProductDto>> GetPurchasedAsync(string userId, int max = 4);

    /// <summary>Lo que más se vende en la tienda (o lo más reciente si todavía no hay ventas).</summary>
    Task<List<SearchProductDto>> GetPopularAsync(int max = 4);

    Task<List<string>> GetTopCategoriesAsync(int max = 8);
}

/// <summary>
/// Buscador en memoria. Las bebidas activas se cargan una vez y se comparten entre todos los
/// clientes (caché de 45 s, o al instante si el admin cambia un producto), así que cada
/// letra que escribe el cliente se resuelve sin tocar la base de datos.
/// </summary>
public class ProductSearchService : IProductSearchService
{
    private sealed class Entry
    {
        public SearchProductDto Dto = null!;
        public string NormName = string.Empty;
        public string[] NameWords = Array.Empty<string>();
        public string[] BrandWords = Array.Empty<string>();
        public string[] CategoryWords = Array.Empty<string>();
        public string[] SizeWords = Array.Empty<string>();
    }

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly TimeSpan IndexTtl = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PopularTtl = TimeSpan.FromMinutes(5);

    private static List<Entry>? _index;
    private static Dictionary<int, Entry> _byId = new();
    private static DateTime _indexAt = DateTime.MinValue;
    private static List<int>? _popularIds;
    private static DateTime _popularAt = DateTime.MinValue;

    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ProductSearchService(IDbContextFactory<ApplicationDbContext> factory)
    {
        _factory = factory;
    }

    /// <summary>Se llama cuando el admin crea, edita u oculta una bebida.</summary>
    public static void Invalidate()
    {
        _indexAt = DateTime.MinValue;
        _popularAt = DateTime.MinValue;
    }

    public async Task WarmUpAsync() => await GetIndexAsync();

    // ── Índice ───────────────────────────────────────────────────────────

    private async Task<List<Entry>> GetIndexAsync()
    {
        var current = _index;
        if (current != null && DateTime.UtcNow - _indexAt < IndexTtl)
            return current;

        await Gate.WaitAsync();
        try
        {
            if (_index != null && DateTime.UtcNow - _indexAt < IndexTtl)
                return _index;

            try
            {
                await using var ctx = await _factory.CreateDbContextAsync();
                var products = await ctx.Products.AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductStores)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                var list = products.Select(p =>
                {
                    var store = p.ProductStores?.FirstOrDefault(ps => ps.StoreId == 1)
                             ?? p.ProductStores?.FirstOrDefault();

                    var dto = new SearchProductDto
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        Brand = p.Brand?.Name,
                        Category = p.Category?.Name ?? string.Empty,
                        Size = p.Size,
                        Price = store?.Price ?? 0,
                        ImageUrl = p.ImageUrl,
                        InStock = (store?.Stock ?? 0) > 0
                    };

                    return new Entry
                    {
                        Dto = dto,
                        NormName = Normalize(dto.Name),
                        NameWords = Words(dto.Name),
                        BrandWords = Words(dto.Brand),
                        CategoryWords = Words(dto.Category),
                        SizeWords = Words(dto.Size)
                    };
                }).ToList();

                _byId = list.ToDictionary(e => e.Dto.ProductId);
                _index = list;
                _indexAt = DateTime.UtcNow;
                return list;
            }
            catch when (_index != null)
            {
                // Si la base falla un momento, se sigue buscando con lo último que se cargó.
                return _index;
            }
        }
        finally
        {
            Gate.Release();
        }
    }

    // ── Búsqueda ─────────────────────────────────────────────────────────

    public async Task<List<SearchProductDto>> SearchAsync(string query, ISet<int>? favoriteIds = null, int max = 8)
    {
        var tokens = Words(query);
        if (tokens.Length == 0) return new List<SearchProductDto>();

        var normQuery = string.Join(' ', tokens);
        var index = await GetIndexAsync();

        var scored = new List<(Entry E, double Score)>();

        foreach (var e in index)
        {
            double total = 0;
            var allMatched = true;

            foreach (var token in tokens)
            {
                var s = TokenScore(token, e);
                if (s <= 0) { allMatched = false; break; }
                total += s;
            }

            if (!allMatched) continue;

            if (e.NormName.StartsWith(normQuery, StringComparison.Ordinal)) total += 6;
            else if (e.NormName.Contains(normQuery, StringComparison.Ordinal)) total += 2;

            if (favoriteIds != null && favoriteIds.Contains(e.Dto.ProductId)) total += 2;
            if (e.Dto.InStock) total += 0.5;

            scored.Add((e, total));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.E.Dto.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.E.Dto.Size, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(x => x.E.Dto)
            .ToList();
    }

    private static double TokenScore(string token, Entry e)
    {
        var name = FieldScore(token, e.NameWords) * 3;
        var brand = FieldScore(token, e.BrandWords) * 2;
        var category = FieldScore(token, e.CategoryWords) * 1.5;
        var size = FieldScore(token, e.SizeWords);
        return Math.Max(Math.Max(name, brand), Math.Max(category, size));
    }

    private static double FieldScore(string token, string[] words)
    {
        double best = 0;

        foreach (var w in words)
        {
            double s = 0;

            if (w == token) s = 4;
            else if (w.StartsWith(token, StringComparison.Ordinal)) s = 3;
            else if (token.Length >= 3 && w.Contains(token, StringComparison.Ordinal)) s = 1.5;
            else if (token.Length >= 4)
            {
                // Errores de escritura: "corna" → Corona, "heinken" → Heineken
                var limit = token.Length >= 6 ? 2 : 1;
                if (Levenshtein(token, w, limit) <= limit) s = 2;
                else if (w.Length > token.Length && Levenshtein(token, w[..token.Length], 1) <= 1) s = 1.8;
            }

            if (s > best) best = s;
        }

        return best;
    }

    private static int Levenshtein(string a, string b, int limit)
    {
        if (Math.Abs(a.Length - b.Length) > limit) return limit + 1;

        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;

        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            var rowMin = cur[0];

            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                if (cur[j] < rowMin) rowMin = cur[j];
            }

            if (rowMin > limit) return limit + 1;
            (prev, cur) = (cur, prev);
        }

        return prev[b.Length];
    }

    // ── Memoria del cliente (lo que ya compró) y populares ───────────────

    public async Task<List<SearchProductDto>> GetPurchasedAsync(string userId, int max = 4)
    {
        if (string.IsNullOrWhiteSpace(userId)) return new List<SearchProductDto>();

        await GetIndexAsync();

        await using var ctx = await _factory.CreateDbContextAsync();
        var rows = await ctx.OrderItems.AsNoTracking()
            .Where(oi => oi.Order.UserId == userId && oi.Order.OrderStatus != "Cancelled")
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Times = g.Count(),
                Last = g.Max(oi => oi.Order.CreatedAt)
            })
            .OrderByDescending(x => x.Times)
            .ThenByDescending(x => x.Last)
            .Take(max * 3)
            .ToListAsync();

        var byId = _byId;
        return rows
            .Where(r => byId.ContainsKey(r.ProductId))
            .Select(r => byId[r.ProductId].Dto)
            .Take(max)
            .ToList();
    }

    public async Task<List<SearchProductDto>> GetPopularAsync(int max = 4)
    {
        var index = await GetIndexAsync();

        var ids = _popularIds;
        if (ids == null || DateTime.UtcNow - _popularAt > PopularTtl)
        {
            try
            {
                await using var ctx = await _factory.CreateDbContextAsync();
                ids = await ctx.OrderItems.AsNoTracking()
                    .GroupBy(oi => oi.ProductId)
                    .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) })
                    .OrderByDescending(x => x.Qty)
                    .Take(24)
                    .Select(x => x.ProductId)
                    .ToListAsync();
            }
            catch
            {
                ids = new List<int>();
            }

            _popularIds = ids;
            _popularAt = DateTime.UtcNow;
        }

        var byId = _byId;
        var result = ids
            .Where(byId.ContainsKey)
            .Select(id => byId[id].Dto)
            .Take(max)
            .ToList();

        if (result.Count < max)
        {
            var used = result.Select(r => r.ProductId).ToHashSet();
            result.AddRange(index
                .Where(e => e.Dto.InStock && !used.Contains(e.Dto.ProductId))
                .Select(e => e.Dto)
                .Take(max - result.Count));
        }

        return result;
    }

    public async Task<List<string>> GetTopCategoriesAsync(int max = 8)
    {
        var index = await GetIndexAsync();
        return index
            .Where(e => !string.IsNullOrWhiteSpace(e.Dto.Category))
            .GroupBy(e => e.Dto.Category)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(g => g.Key)
            .ToList();
    }

    // ── Texto: sin tildes, sin mayúsculas ────────────────────────────────

    /// <summary>"Piña Colada" → "pina colada". Quita tildes, pasa a minúsculas y limpia espacios.</summary>
    public static string Normalize(string? s) => string.Join(' ', Words(s));

    private static string[] Words(string? s) =>
        Fold(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    // Mismo largo que el original (carácter a carácter), para poder resaltar sobre el texto real.
    private static string Fold(string? s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        var sb = new StringBuilder(s.Length);
        foreach (var c in s) sb.Append(FoldChar(c));
        return sb.ToString();
    }

    private static char FoldChar(char c)
    {
        if (!char.IsLetterOrDigit(c)) return ' ';

        var lower = char.ToLowerInvariant(c);
        if (lower < 128) return lower;

        foreach (var ch in lower.ToString().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                return ch;
        }

        return lower;
    }

    /// <summary>Parte el nombre en trozos para pintar en dorado lo que coincide con lo escrito.</summary>
    public static List<HighlightPart> Highlight(string text, string query)
    {
        var parts = new List<HighlightPart>();
        if (string.IsNullOrEmpty(text)) return parts;

        var tokens = Words(query);
        if (tokens.Length == 0)
        {
            parts.Add(new HighlightPart(text, false));
            return parts;
        }

        var folded = Fold(text);
        var hit = new bool[text.Length];

        foreach (var token in tokens)
        {
            var from = 0;
            while (from < folded.Length)
            {
                var i = folded.IndexOf(token, from, StringComparison.Ordinal);
                if (i < 0) break;
                for (var k = i; k < i + token.Length; k++) hit[k] = true;
                from = i + token.Length;
            }
        }

        var start = 0;
        for (var i = 1; i <= text.Length; i++)
        {
            if (i == text.Length || hit[i] != hit[start])
            {
                parts.Add(new HighlightPart(text.Substring(start, i - start), hit[start]));
                start = i;
            }
        }

        return parts;
    }
}
