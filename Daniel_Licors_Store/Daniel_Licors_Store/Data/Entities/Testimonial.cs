namespace Daniel_Licors_Store.Data.Entities;

public class Testimonial
{
    public int TestimonialId { get; set; }
    public string? CustomerName { get; set; }
    public string? Comment { get; set; }
    public int? Rating { get; set; }
    public bool IsActive { get; set; }
}