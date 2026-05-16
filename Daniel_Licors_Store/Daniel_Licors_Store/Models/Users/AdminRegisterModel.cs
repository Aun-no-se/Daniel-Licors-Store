namespace Daniel_Licors_Store.Models.Users.Auth
{
    public class AdminRegisterModel : RegisterModel
    {
        public int RoleId { get; set; } // Admin, Empleado, Cliente
    }
}