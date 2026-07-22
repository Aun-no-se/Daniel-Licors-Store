namespace Daniel_Licors_Store.Data.Entities
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();

        public string FullName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int Age { get; set; }

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── Bloqueo de cuenta por intentos fallidos de login ────────────
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEnd { get; set; }

        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}