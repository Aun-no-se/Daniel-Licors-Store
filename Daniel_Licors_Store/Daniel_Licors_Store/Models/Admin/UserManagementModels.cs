namespace Daniel_Licors_Store.Models.Admin
{
    // ── Resumen de cuentas por rol ──────────────────────────────────────────
    public class UserRoleSummary
    {
        public string RoleName  { get; set; } = string.Empty;
        public int    Total     { get; set; }
        public int    Active    { get; set; }
        public int    Inactive  { get; set; }
    }

    // ── Fila completa de un usuario con su actividad ────────────────────────
    public class UserDetailDto
    {
        public string   UserId          { get; set; } = string.Empty;
        public string   FullName        { get; set; } = string.Empty;
        public string   LastName        { get; set; } = string.Empty;
        public string   Email           { get; set; } = string.Empty;
        public string?  Phone           { get; set; }
        public int      Age             { get; set; }
        public bool     IsActive        { get; set; }
        public DateTime CreatedAt       { get; set; }

        // Rol
        public string   RoleName        { get; set; } = string.Empty;
        public DateTime AssignedAt      { get; set; }

        // Auditoría
        public int      TotalActions    { get; set; }
        public string?  LastAction      { get; set; }
        public string?  LastEntity      { get; set; }
        public string?  LastEntityId    { get; set; }
        public DateTime? LastActivityAt { get; set; }

        // Pedidos
        public int      TotalOrders     { get; set; }

        // Helper visual
        public string   Initials => string.IsNullOrEmpty(FullName)
            ? "?"
            : $"{FullName[0]}{(LastName.Length > 0 ? LastName[0] : ' ')}".Trim();
    }

    // ── Log de auditoría expandido ──────────────────────────────────────────
    public class AuditLogDto
    {
        public int      AuditLogId  { get; set; }
        public string   UserId      { get; set; } = string.Empty;
        public string   UserName    { get; set; } = string.Empty;
        public string   RoleName    { get; set; } = string.Empty;
        public string?  Action      { get; set; }
        public string?  Entity      { get; set; }
        public string?  EntityId    { get; set; }
        public DateTime CreatedAt   { get; set; }
    }

    // ── Filtros para la tabla ────────────────────────────────────────────────
    public class UserFilterModel
    {
        public string? SearchText   { get; set; }
        public string? RoleFilter   { get; set; }   // null = todos
        public string? StatusFilter { get; set; }   // "active" | "inactive" | null
        public string? SortBy       { get; set; } = "LastActivity"; // LastActivity | Name | Created
    }
}
