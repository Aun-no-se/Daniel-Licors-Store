/* ═══════════════════════════════════════════════════════════════════════
   03 · VERIFICAR QUE LA BASE COINCIDE CON EL CÓDIGO  —  Daniel Liquor Store

   Compara las tablas y columnas que usa el proyecto (Entity Framework) contra
   la base de datos real. Solo lee: no cambia nada. Córrelo cuando salga un
   error "Invalid column name" o después de actualizar el proyecto.

   Resultado 1: columnas/tablas que el código usa y NO existen  (debe salir VACÍO).
                Si sale algo, corre 01-crear-base-de-datos.sql: agrega lo que falte.
   Resultado 2: columnas obligatorias que el código no conoce     (debe salir VACÍO).
   Resultado 3: tablas de la base que el código no usa            (solo informativo).
   ═══════════════════════════════════════════════════════════════════════ */
USE DanielLicorsStoreDB;
GO

IF OBJECT_ID('tempdb..#esperado') IS NOT NULL DROP TABLE #esperado;
SELECT * INTO #esperado
FROM (VALUES
    ('AuditLogs', 'AuditLogId'),
    ('AuditLogs', 'UserId'),
    ('AuditLogs', 'Action'),
    ('AuditLogs', 'Entity'),
    ('AuditLogs', 'EntityId'),
    ('AuditLogs', 'CreatedAt'),
    ('Brands', 'BrandId'),
    ('Brands', 'Name'),
    ('Brands', 'IsActive'),
    ('Carts', 'CartId'),
    ('Carts', 'UserId'),
    ('Carts', 'SessionId'),
    ('Carts', 'StoreId'),
    ('Carts', 'CartStatus'),
    ('Carts', 'CreatedAt'),
    ('Carts', 'UpdatedAt'),
    ('CartCombos', 'CartComboId'),
    ('CartCombos', 'CartId'),
    ('CartCombos', 'ComboId'),
    ('CartCombos', 'Quantity'),
    ('CartCombos', 'UnitPrice'),
    ('CartCombos', 'SubTotal'),
    ('CartItems', 'CartItemId'),
    ('CartItems', 'CartId'),
    ('CartItems', 'ProductId'),
    ('CartItems', 'Quantity'),
    ('CartItems', 'UnitPrice'),
    ('CartItems', 'SubTotal'),
    ('CartPromotions', 'CartPromotionId'),
    ('CartPromotions', 'CartId'),
    ('CartPromotions', 'PromotionId'),
    ('CartPromotions', 'Quantity'),
    ('CartPromotions', 'UnitPrice'),
    ('CartPromotions', 'SubTotal'),
    ('Categories', 'CategoryId'),
    ('Categories', 'Name'),
    ('Categories', 'IsActive'),
    ('Combos', 'ComboId'),
    ('Combos', 'Name'),
    ('Combos', 'Description'),
    ('Combos', 'ComboPrice'),
    ('Combos', 'ImageUrl'),
    ('Combos', 'StartDate'),
    ('Combos', 'EndDate'),
    ('Combos', 'IsActive'),
    ('ComboProducts', 'ComboProductId'),
    ('ComboProducts', 'ComboId'),
    ('ComboProducts', 'ProductId'),
    ('ComboProducts', 'Quantity'),
    ('Orders', 'OrderId'),
    ('Orders', 'UserId'),
    ('Orders', 'StoreId'),
    ('Orders', 'OrderStatus'),
    ('Orders', 'PaymentMethod'),
    ('Orders', 'TotalAmount'),
    ('Orders', 'CreatedAt'),
    ('OrderItems', 'OrderItemId'),
    ('OrderItems', 'OrderId'),
    ('OrderItems', 'ProductId'),
    ('OrderItems', 'Quantity'),
    ('OrderItems', 'UnitPrice'),
    ('Products', 'ProductId'),
    ('Products', 'Name'),
    ('Products', 'Description'),
    ('Products', 'AgingYears'),
    ('Products', 'CategoryId'),
    ('Products', 'BrandId'),
    ('Products', 'IsPremium'),
    ('Products', 'IsActive'),
    ('Products', 'ImageUrl'),
    ('Products', 'CreatedAt'),
    ('Products', 'Size'),
    ('Products', 'SizeMl'),
    ('ProductStores', 'ProductStoreId'),
    ('ProductStores', 'ProductId'),
    ('ProductStores', 'StoreId'),
    ('ProductStores', 'Price'),
    ('ProductStores', 'Stock'),
    ('ProductStores', 'IsAvailable'),
    ('ProductStores', 'RowVersion'),
    ('Promotions', 'PromotionId'),
    ('Promotions', 'Title'),
    ('Promotions', 'Description'),
    ('Promotions', 'StartDate'),
    ('Promotions', 'EndDate'),
    ('Promotions', 'PromotionType'),
    ('Promotions', 'ImageUrl'),
    ('Promotions', 'IsActive'),
    ('PromotionProducts', 'PromotionProductId'),
    ('PromotionProducts', 'PromotionId'),
    ('PromotionProducts', 'ProductId'),
    ('PromotionProducts', 'SpecialPrice'),
    ('PromotionProducts', 'DiscountPercentage'),
    ('PromotionProducts', 'RequiredQuantity'),
    ('PromotionProducts', 'PayQuantity'),
    ('Roles', 'RoleId'),
    ('Roles', 'Name'),
    ('Roles', 'Description'),
    ('Roles', 'IsActive'),
    ('StockMovements', 'StockMovementId'),
    ('StockMovements', 'ProductId'),
    ('StockMovements', 'StoreId'),
    ('StockMovements', 'Quantity'),
    ('StockMovements', 'MovementType'),
    ('StockMovements', 'UserId'),
    ('StockMovements', 'CreatedAt'),
    ('Stores', 'StoreId'),
    ('Stores', 'Name'),
    ('Stores', 'Address'),
    ('Stores', 'Phone'),
    ('Stores', 'WhatsApp'),
    ('Stores', 'OpenTime'),
    ('Stores', 'CloseTime'),
    ('Stores', 'IsActive'),
    ('Users', 'UserId'),
    ('Users', 'FullName'),
    ('Users', 'LastName'),
    ('Users', 'Age'),
    ('Users', 'Email'),
    ('Users', 'PasswordHash'),
    ('Users', 'IsActive'),
    ('Users', 'CreatedAt'),
    ('Users', 'FailedLoginAttempts'),
    ('Users', 'LockoutEnd'),
    ('UserRoles', 'UserRoleId'),
    ('UserRoles', 'UserId'),
    ('UserRoles', 'RoleId'),
    ('UserRoles', 'AssignedAt')
) AS v (Tabla, Columna);

-- 1) Lo que el código espera y no está en la base
SELECT e.Tabla, e.Columna AS ColumnaFaltante
FROM #esperado e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
       ON c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = e.Tabla AND c.COLUMN_NAME = e.Columna
WHERE c.COLUMN_NAME IS NULL
ORDER BY e.Tabla, e.Columna;

-- 2) Columnas obligatorias (NOT NULL, sin valor por defecto) que el código no usa: harían fallar los INSERT
SELECT c.TABLE_NAME AS Tabla, c.COLUMN_NAME AS ColumnaObligatoriaSinUso, c.DATA_TYPE AS Tipo
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_SCHEMA = 'dbo'
  AND c.TABLE_NAME IN (SELECT DISTINCT Tabla FROM #esperado)
  AND NOT EXISTS (SELECT 1 FROM #esperado e WHERE e.Tabla = c.TABLE_NAME AND e.Columna = c.COLUMN_NAME)
  AND c.IS_NULLABLE = 'NO'
  AND c.COLUMN_DEFAULT IS NULL
  AND COLUMNPROPERTY(OBJECT_ID('dbo.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') = 0
  AND COLUMNPROPERTY(OBJECT_ID('dbo.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsComputed') = 0
ORDER BY c.TABLE_NAME, c.COLUMN_NAME;

-- 3) Tablas que existen en la base pero el código no usa (informativo)
SELECT t.TABLE_NAME AS TablaSinUsoEnElCodigo
FROM INFORMATION_SCHEMA.TABLES t
WHERE t.TABLE_SCHEMA = 'dbo' AND t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_NAME NOT IN (SELECT DISTINCT Tabla FROM #esperado)
ORDER BY t.TABLE_NAME;

DROP TABLE #esperado;
GO
