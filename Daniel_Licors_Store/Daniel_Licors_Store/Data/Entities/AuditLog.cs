namespace Daniel_Licors_Store.Data.Entities;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public string? UserId { get; set; }
    public string? Action { get; set; }
    public string? Entity { get; set; }
    public string? EntityId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
}