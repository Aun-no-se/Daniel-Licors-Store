namespace Daniel_Licors_Store.Models.Combo
{
    public class ComboDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal ComboPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}
