/* ═══════════════════════════════════════════════════════════════════════
   04 · AUDITORÍA DE CUENTAS Y ACCIONES  —  Daniel Liquor Store
   Consultas para revisar quién hizo qué (tabla AuditLogs) y cómo están las
   cuentas por rol. Todas son de lectura, salvo el bloque OPCIONAL del final
   (datos de prueba) que viene comentado.
   ═══════════════════════════════════════════════════════════════════════ */
USE DanielLicorsStoreDB;
GO

-- 1) Todos los registros de auditoría (los más recientes primero)
SELECT
    al.AuditLogId,
    u.FullName   AS Usuario,
    r.Name       AS Rol,
    al.Action    AS Accion,
    al.Entity    AS Entidad,
    al.EntityId  AS IdEntidad,
    al.CreatedAt AS FechaHora
FROM AuditLogs al
LEFT JOIN Users u      ON al.UserId = u.UserId
LEFT JOIN UserRoles ur ON u.UserId  = ur.UserId
LEFT JOIN Roles r      ON ur.RoleId = r.RoleId
ORDER BY al.CreatedAt DESC;
GO

-- 2) Usuarios con su rol (para ver los UserId reales)
SELECT u.UserId, u.FullName, r.Name AS Rol
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r      ON ur.RoleId = r.RoleId;
GO

-- 3) Resumen rápido por rol (lo que calcula la página de auditoría)
SELECT
    r.Name AS Rol,
    COUNT(u.UserId)                                  AS Total,
    SUM(CASE WHEN u.IsActive = 1 THEN 1 ELSE 0 END)  AS Activos,
    SUM(CASE WHEN u.IsActive = 0 THEN 1 ELSE 0 END)  AS Inactivos
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r      ON ur.RoleId = r.RoleId
GROUP BY r.Name
ORDER BY r.Name;
GO

-- 4) Los 10 usuarios con más acciones
SELECT TOP 10
    u.FullName,
    r.Name AS Rol,
    COUNT(al.AuditLogId) AS TotalAcciones,
    MAX(al.CreatedAt)    AS UltimaAccion
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r      ON ur.RoleId = r.RoleId
LEFT JOIN AuditLogs al  ON u.UserId = al.UserId
GROUP BY u.UserId, u.FullName, r.Name
ORDER BY TotalAcciones DESC;
GO

/* ───────────────────────────────────────────────────────────────────────
   5) OPCIONAL — registros de prueba para ver la pantalla de auditoría.
      Está comentado para que no se inserte por accidente: borra las marcas de
      comentario de los extremos del bloque y ejecútalo solo si quieres datos falsos.
      Busca solos un Admin, un Empleado y un Cliente (si no existe alguno de
      esos roles, esa acción queda sin usuario).
   ─────────────────────────────────────────────────────────────────────── */
/*
DECLARE @AdminId    NVARCHAR(450) = (SELECT TOP 1 ur.UserId FROM UserRoles ur INNER JOIN Roles r ON r.RoleId = ur.RoleId WHERE r.Name = 'Admin');
DECLARE @EmpleadoId NVARCHAR(450) = (SELECT TOP 1 ur.UserId FROM UserRoles ur INNER JOIN Roles r ON r.RoleId = ur.RoleId WHERE r.Name = 'Empleado');
DECLARE @ClienteId  NVARCHAR(450) = (SELECT TOP 1 ur.UserId FROM UserRoles ur INNER JOIN Roles r ON r.RoleId = ur.RoleId WHERE r.Name = 'Cliente');

INSERT INTO AuditLogs (UserId, Action, Entity, EntityId, CreatedAt) VALUES
(@AdminId,    'Creó cuenta',     'User',      ISNULL(@EmpleadoId, ''), DATEADD(HOUR,   -5, GETDATE())),
(@AdminId,    'Editó producto',  'Product',   '3',                     DATEADD(HOUR,   -3, GETDATE())),
(@AdminId,    'Creó combo',      'Combo',     '1',                     DATEADD(HOUR,   -2, GETDATE())),
(@EmpleadoId, 'Actualizó stock', 'Product',   '5',                     DATEADD(MINUTE, -90, GETDATE())),
(@EmpleadoId, 'Editó promoción', 'Promotion', '2',                     DATEADD(MINUTE, -30, GETDATE())),
(@ClienteId,  'Envió pedido',    'Cart',      '1',                     DATEADD(MINUTE, -10, GETDATE()));
*/
GO
