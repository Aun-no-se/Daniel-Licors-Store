namespace Daniel_Licors_Store.Models.Combo
{
    public class ComboAdminDto
    {
        public int ComboId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ComboPrice { get; set; }
        public string ImageUrl { get; set; } = "/images/combo-default.png";
        public string ProductsSummary { get; set; } = string.Empty;
    }
}