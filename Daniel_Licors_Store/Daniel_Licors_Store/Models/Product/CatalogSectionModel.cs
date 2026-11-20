namespace Daniel_Licors_Store.Models.Product;

/// <summary>
/// Una sección del index: una categoría o una marca con sus productos.
/// La arma CatalogService a partir de lo que está registrado en la base de datos.
/// </summary>
public class CatalogSectionModel
{
    /// <summary>"categoria" | "marca"</summary>
    public string Kind { get; set; } = "categoria";
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>Categoría principal de una marca (para escoger el ícono del título).</summary>
    public string? IconHint { get; set; }

    public List<ProductResponseModel> Products { get; set; } = new();

    /// <summary>Id HTML de la sección; coincide con el data-target de los chips del marquee.</summary>
    public string AnchorId => $"{Kind}-{Id}";
}
