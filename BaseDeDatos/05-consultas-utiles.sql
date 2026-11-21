/* ═══════════════════════════════════════════════════════════════════════
   05 · CONSULTAS ÚTILES DEL DÍA A DÍA  —  Daniel Liquor Store
   Todas son de lectura: no cambian ningún dato. Selecciona la consulta que
   quieras y dale F5 (o córrelas todas).
   ═══════════════════════════════════════════════════════════════════════ */
USE DanielLicorsStoreDB;
GO

-- 1) Usuarios y sus roles
SELECT
    u.UserId, u.FullName, u.LastName, u.Email, u.Phone,
    u.IsActive AS UserIsActive,
    r.RoleId, r.Name AS RoleName, r.Description AS RoleDescription,
    ur.AssignedAt
FROM Users u
INNER JOIN UserRoles ur ON u.UserId  = ur.UserId
INNER JOIN Roles r      ON ur.RoleId = r.RoleId;
GO

-- 2) Intentos fallidos de inicio de sesión y bloqueos
SELECT UserId, FullName, Email, FailedLoginAttempts, LockoutEnd
FROM Users
ORDER BY FailedLoginAttempts DESC, FullName;
GO

-- 3) Catálogo: cada bebida con su tamaño, precio y stock
SELECT
    p.ProductId, p.Name, p.Size,
    c.Name AS Categoria, b.Name AS Marca,
    ps.StoreId, ps.Price, ps.Stock, ps.IsAvailable,
    p.IsActive AS BebidaActiva
FROM Products p
INNER JOIN Categories c    ON c.CategoryId = p.CategoryId
LEFT  JOIN Brands b        ON b.BrandId    = p.BrandId
LEFT  JOIN ProductStores ps ON ps.ProductId = p.ProductId
ORDER BY c.Name, p.Name, p.SizeMl, p.Size;
GO

-- 4) Stock con su control de concurrencia (RowVersion)
SELECT ps.ProductStoreId, ps.ProductId, p.Name, p.Size, ps.StoreId, ps.Stock, ps.RowVersion
FROM ProductStores ps
INNER JOIN Products p ON p.ProductId = ps.ProductId
ORDER BY ps.ProductStoreId;
GO

-- 5) Bebidas repetidas: mismo nombre, categoría, marca y tamaño (no debería salir nada)
SELECT Name, CategoryId, BrandId, Size, COUNT(*) AS Veces
FROM Products
GROUP BY Name, CategoryId, BrandId, Size
HAVING COUNT(*) > 1;
GO

-- 6) Bebidas que todavía traen el tamaño escrito en el nombre (ej. "Brugal Añejo 750ml"):
--    entra a Editar, quita el tamaño del nombre y elígelo con los botones de tamaño
SELECT ProductId, Name
FROM Products
WHERE Size IS NULL
  AND (   Name LIKE '%[0-9]ml%'  OR Name LIKE '%[0-9] ml%'
       OR Name LIKE '%[0-9]oz%'  OR Name LIKE '%[0-9] oz%'
       OR Name LIKE '%[0-9]lt%'  OR Name LIKE '%[0-9] lt%'
       OR Name LIKE '%[0-9]L'    OR Name LIKE '%[0-9] L'
       OR Name LIKE '%[0-9]L %'  OR Name LIKE '%[0-9] L %' )
ORDER BY Name;
GO

-- 7) Cuántas filas tiene cada tabla
SELECT t.name AS Tabla, SUM(p.rows) AS Filas
FROM sys.tables t
INNER JOIN sys.partitions p ON p.object_id = t.object_id AND p.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name;
GO
