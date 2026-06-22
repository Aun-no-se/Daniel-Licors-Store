namespace Daniel_Licors_Store.Models.Users.Auth;

public class UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public DateTime LastActivity { get; set; } = DateTime.Now;
}
