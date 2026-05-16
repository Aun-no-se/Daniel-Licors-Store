namespace Daniel_Licors_Store.Data.Entities
{
    public class UserRole
    {
        public int UserRoleId { get; set; }

        public string UserId { get; set; } = string.Empty;
        public User User { get; set; } = null!;

        public int RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public DateTime AssignedAt { get; set; } = DateTime.Now;
    }
}