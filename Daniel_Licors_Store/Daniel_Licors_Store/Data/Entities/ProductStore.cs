namespace Daniel_Licors_Store.Data.Entities;

public class ProductStore
{
    public int ProductStoreId { get; set; }

    public int ProductId { get; set; }
    public int StoreId { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }

    // Token de concurrencia (SQL Server ROWVERSION). EF Core lo compara
    // automáticamente en cada SaveChanges: si otra venta ya modificó esta
    // misma fila entre que la leímos y que intentamos guardar, lanza
    // DbUpdateConcurrencyException en vez de dejar pasar un descuento de
    // stock basado en datos viejos. Nunca se asigna a mano — SQL Server
    // lo genera solo en cada UPDATE.
    public byte[] RowVersion { get; set; } = default!;

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
