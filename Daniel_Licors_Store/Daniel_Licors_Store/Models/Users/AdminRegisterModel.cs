namespace Daniel_Licors_Store.Models.Users.Auth;

public class AdminRegisterModel
{
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public int RoleId { get; set; }
}
