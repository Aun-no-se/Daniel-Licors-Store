namespace Daniel_Licors_Store.Models.Product;

/// <summary>Una bebida tal como la muestra el buscador (ligera: solo lo que se ve en la fila).</summary>
public class SearchProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Size { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool InStock { get; set; }
}

/// <summary>Un pedazo de texto del nombre; IsHit = coincide con lo que el cliente escribió.</summary>
public readonly record struct HighlightPart(string Text, bool IsHit);
