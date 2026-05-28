namespace Daniel_Licors_Store.Models.Combo
{
    public class ComboCreateModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ComboPrice { get; set; }

        //NUEVA PROPIEDAD: Para recibir la ruta de la foto asignada en el UploadImageAsync()
        public string? ImageUrl { get; set; }

        public List<ComboProductTemp> Products { get; set; } = new();
    }
}