/* ═══════════════════════════════════════════════════════════════════════
   01 · CREAR LA BASE DE DATOS  —  Daniel Liquor Store
   Base: DanielLicorsStoreDB  ·  SQL Server

   Con un solo F5 deja todo listo: la base de datos, las 24 tablas con sus
   relaciones, los datos iniciales (3 roles, sucursal Haina, 12 categorías,
   15 marcas) y una revisión final contra lo que espera el código.

   · Se puede correr varias veces: solo crea lo que no exista, no borra nada
     y no duplica los datos iniciales.
   · Si la base ya existe con una versión anterior, también la pone al día
     (agrega las columnas que le falten: tamaños, promociones, bloqueo de
     usuarios, control de stock). Por eso reemplaza a esos scripts sueltos.
   · Empezar de cero:  02-borrar-base-de-datos.sql  →  este script.

   AL FINAL deben salir 2 tablas de resultados VACÍAS y un resumen con
   12 categorías, 15 marcas, 3 roles y 1 sucursal.
   ═══════════════════════════════════════════════════════════════════════ */

IF DB_ID('DanielLicorsStoreDB') IS NULL
    CREATE DATABASE DanielLicorsStoreDB;
GO

USE DanielLicorsStoreDB;
GO

/* ───────────────────────── CATÁLOGO ───────────────────────── */
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories (
        CategoryId INT IDENTITY PRIMARY KEY,
        Name       NVARCHAR(100) NOT NULL,
        IsActive   BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.Brands', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Brands (
        BrandId  INT IDENTITY PRIMARY KEY,
        Name     NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        ProductId   INT IDENTITY PRIMARY KEY,
        Name        NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        BrandId     INT NULL,
        CategoryId  INT NOT NULL,
        IsPremium   BIT NOT NULL DEFAULT 0,
        ImageUrl    NVARCHAR(500) NULL,
        IsActive    BIT NOT NULL DEFAULT 1,
        CreatedAt   DATETIME NOT NULL DEFAULT GETDATE(),
        AgingYears  INT NULL,
        Size        NVARCHAR(40) NULL,      -- tamaño: "355 ml", "750 ml", "1 L"…
        SizeMl      INT NULL,               -- volumen en ml (solo para ordenar)

        CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId),
        CONSTRAINT FK_Product_Brand    FOREIGN KEY (BrandId)    REFERENCES dbo.Brands(BrandId)
    );
END
GO
/* ───────────────────────── SUCURSALES ───────────────────────── */
IF OBJECT_ID('dbo.Stores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Stores (
        StoreId   INT IDENTITY PRIMARY KEY,
        Name      NVARCHAR(150) NOT NULL,
        Address   NVARCHAR(250) NOT NULL,
        Phone     NVARCHAR(20) NULL,
        WhatsApp  NVARCHAR(20) NULL,
        OpenTime  TIME NULL,
        CloseTime TIME NULL,
        IsActive  BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.ProductStores', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductStores (
        ProductStoreId INT IDENTITY PRIMARY KEY,
        ProductId      INT NOT NULL,
        StoreId        INT NOT NULL,
        Price          DECIMAL(10,2) NOT NULL,
        Stock          INT NOT NULL DEFAULT 0,
        IsAvailable    BIT NOT NULL DEFAULT 1,
        RowVersion     ROWVERSION,

        CONSTRAINT FK_ProductStore_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT FK_ProductStore_Store   FOREIGN KEY (StoreId)   REFERENCES dbo.Stores(StoreId),
        CONSTRAINT UQ_Product_Store UNIQUE (ProductId, StoreId)
    );
END
GO
IF OBJECT_ID('dbo.BestSellers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BestSellers (
        BestSellerId  INT IDENTITY PRIMARY KEY,
        ProductId     INT NOT NULL,
        OrderPriority INT NOT NULL DEFAULT 1,
        IsActive      BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_BestSeller_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END
GO
/* ───────────────────────── PROMOCIONES Y COMBOS ───────────────────────── */
IF OBJECT_ID('dbo.Promotions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Promotions (
        PromotionId   INT IDENTITY PRIMARY KEY,
        Title         NVARCHAR(150) NOT NULL,
        Description   NVARCHAR(500) NULL,
        StartDate     DATETIME NOT NULL,
        EndDate       DATETIME NOT NULL,
        PromotionType NVARCHAR(50) NULL,          -- Weekly, Combo, Highlight
        ImageUrl      NVARCHAR(MAX) NULL,
        IsActive      BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.PromotionProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PromotionProducts (
        PromotionProductId INT IDENTITY PRIMARY KEY,
        PromotionId        INT NOT NULL,
        ProductId          INT NOT NULL,
        SpecialPrice       DECIMAL(10,2) NULL,    -- precio especial directo
        DiscountPercentage DECIMAL(18,2) NULL,    -- descuento porcentual
        RequiredQuantity   INT NULL,              -- "Lleva X ..."
        PayQuantity        INT NULL,              -- "... paga Y"

        CONSTRAINT FK_PromotionProduct_Promotion FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(PromotionId),
        CONSTRAINT FK_PromotionProduct_Product   FOREIGN KEY (ProductId)   REFERENCES dbo.Products(ProductId)
    );
END
GO
IF OBJECT_ID('dbo.Combos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Combos (
        ComboId     INT IDENTITY PRIMARY KEY,
        Name        NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        ComboPrice  DECIMAL(10,2) NOT NULL,
        StartDate   DATETIME NULL,
        EndDate     DATETIME NULL,
        ImageUrl    NVARCHAR(500) NULL,
        IsActive    BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.ComboProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComboProducts (
        ComboProductId INT IDENTITY PRIMARY KEY,
        ComboId        INT NOT NULL,
        ProductId      INT NOT NULL,
        Quantity       INT NOT NULL,

        CONSTRAINT FK_ComboProduct_Combo   FOREIGN KEY (ComboId)   REFERENCES dbo.Combos(ComboId) ON DELETE CASCADE,
        CONSTRAINT FK_ComboProduct_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END
GO
/* ───────────────────────── USUARIOS Y ROLES ───────────────────────── */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        UserId              NVARCHAR(450) PRIMARY KEY,
        FullName            NVARCHAR(150) NULL,
        LastName            NVARCHAR(150) NULL,
        Email               NVARCHAR(150) NULL,
        Phone               NVARCHAR(20) NULL,
        Age                 INT NOT NULL DEFAULT 0,
        PasswordHash        NVARCHAR(500) NULL,
        IsActive            BIT NOT NULL DEFAULT 1,
        CreatedAt           DATETIME NOT NULL DEFAULT GETDATE(),
        FailedLoginAttempts INT NOT NULL DEFAULT 0,
        LockoutEnd          DATETIME2 NULL
    );
END
GO
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles (
        RoleId      INT IDENTITY PRIMARY KEY,
        Name        NVARCHAR(50) NOT NULL UNIQUE,
        Description NVARCHAR(200) NULL,
        IsActive    BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles (
        UserRoleId INT IDENTITY PRIMARY KEY,
        UserId     NVARCHAR(450) NOT NULL,
        RoleId     INT NOT NULL,
        AssignedAt DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT UQ_User_Role UNIQUE (UserId, RoleId)
    );
END
GO
/* ───────────────────────── CARRITO ───────────────────────── */
IF OBJECT_ID('dbo.Carts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Carts (
        CartId     INT IDENTITY PRIMARY KEY,
        UserId     NVARCHAR(450) NULL,
        SessionId  NVARCHAR(100) NULL,
        StoreId    INT NOT NULL,
        CartStatus NVARCHAR(20) NOT NULL DEFAULT 'Active',   -- Active, Sent, Abandoned
        CreatedAt  DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt  DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_Cart_User  FOREIGN KEY (UserId)  REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Cart_Store FOREIGN KEY (StoreId) REFERENCES dbo.Stores(StoreId)
    );
END
GO
IF OBJECT_ID('dbo.CartItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems (
        CartItemId INT IDENTITY PRIMARY KEY,
        CartId     INT NOT NULL,
        ProductId  INT NOT NULL,
        Quantity   INT NOT NULL,
        UnitPrice  DECIMAL(10,2) NOT NULL,
        SubTotal   DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_CartItem_Cart    FOREIGN KEY (CartId)    REFERENCES dbo.Carts(CartId),
        CONSTRAINT FK_CartItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END
GO
IF OBJECT_ID('dbo.CartCombos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartCombos (
        CartComboId INT IDENTITY PRIMARY KEY,
        CartId      INT NOT NULL,
        ComboId     INT NOT NULL,
        Quantity    INT NOT NULL,
        UnitPrice   DECIMAL(10,2) NOT NULL,
        SubTotal    DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_CartCombo_Cart  FOREIGN KEY (CartId)  REFERENCES dbo.Carts(CartId),
        CONSTRAINT FK_CartCombo_Combo FOREIGN KEY (ComboId) REFERENCES dbo.Combos(ComboId)
    );
END
GO
IF OBJECT_ID('dbo.CartPromotions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartPromotions (
        CartPromotionId INT IDENTITY PRIMARY KEY,
        CartId          INT NOT NULL,
        PromotionId     INT NOT NULL,
        Quantity        INT NOT NULL,
        UnitPrice       DECIMAL(10,2) NOT NULL,
        SubTotal        DECIMAL(10,2) NOT NULL,

        CONSTRAINT FK_CartPromotion_Cart      FOREIGN KEY (CartId)      REFERENCES dbo.Carts(CartId) ON DELETE CASCADE,
        CONSTRAINT FK_CartPromotion_Promotion FOREIGN KEY (PromotionId) REFERENCES dbo.Promotions(PromotionId)
    );
END
GO
/* ───────────────────────── PEDIDOS ───────────────────────── */
IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        OrderId       INT IDENTITY PRIMARY KEY,
        UserId        NVARCHAR(450) NULL,
        StoreId       INT NOT NULL,
        OrderStatus   NVARCHAR(30) NOT NULL,       -- Pending, InProcess, Completed
        PaymentMethod NVARCHAR(20) NULL,
        TotalAmount   DECIMAL(10,2) NOT NULL,
        CreatedAt     DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_Order_User  FOREIGN KEY (UserId)  REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_Order_Store FOREIGN KEY (StoreId) REFERENCES dbo.Stores(StoreId)
    );
END
GO
IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems (
        OrderItemId INT IDENTITY PRIMARY KEY,
        OrderId     INT NOT NULL,
        ProductId   INT NOT NULL,
        Quantity    INT NOT NULL,
        UnitPrice   DECIMAL(10,2) NOT NULL,
        SubTotal    AS (Quantity * UnitPrice) PERSISTED,   -- calculado: el código no lo envía

        CONSTRAINT FK_OrderItem_Order   FOREIGN KEY (OrderId)   REFERENCES dbo.Orders(OrderId),
        CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END
GO
IF OBJECT_ID('dbo.StockMovements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovements (
        StockMovementId INT IDENTITY PRIMARY KEY,
        ProductId       INT NOT NULL,
        StoreId         INT NOT NULL,
        Quantity        INT NOT NULL,
        MovementType    NVARCHAR(20) NOT NULL,     -- In, Out, Adjustment
        UserId          NVARCHAR(450) NULL,
        CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_StockMovement_Product FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId),
        CONSTRAINT FK_StockMovement_Store   FOREIGN KEY (StoreId)   REFERENCES dbo.Stores(StoreId),
        CONSTRAINT FK_StockMovement_User    FOREIGN KEY (UserId)    REFERENCES dbo.Users(UserId)
    );
END
GO
/* ───────────────────────── CONTENIDO, CONFIGURACIÓN Y AUDITORÍA ───────────────────────── */
IF OBJECT_ID('dbo.Events', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Events (
        EventId     INT IDENTITY PRIMARY KEY,
        Title       NVARCHAR(150) NOT NULL,
        Description NVARCHAR(500) NULL,
        ImageUrl    NVARCHAR(500) NULL,
        StartDate   DATETIME NULL,
        EndDate     DATETIME NULL,
        IsActive    BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.Testimonials', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Testimonials (
        TestimonialId INT IDENTITY PRIMARY KEY,
        CustomerName  NVARCHAR(150) NULL,
        Comment       NVARCHAR(500) NULL,
        Rating        INT NULL,
        IsActive      BIT NOT NULL DEFAULT 1
    );
END
GO
IF OBJECT_ID('dbo.SiteConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SiteConfigs (
        SiteConfigId INT IDENTITY PRIMARY KEY,
        ConfigKey    NVARCHAR(100) NOT NULL,
        ConfigValue  NVARCHAR(MAX) NOT NULL
    );
END
GO
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs (
        AuditLogId INT IDENTITY PRIMARY KEY,
        UserId     NVARCHAR(450) NULL,
        Action     NVARCHAR(200) NULL,
        Entity     NVARCHAR(100) NULL,
        EntityId   NVARCHAR(100) NULL,
        CreatedAt  DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END
GO
/* ═══════════════════════════════════════════════════════════════════════
   AL DÍA PARA BASES CREADAS CON VERSIONES ANTERIORES DEL SCRIPT
   (en una base nueva, todo esto no hace nada)
   ═══════════════════════════════════════════════════════════════════════ */
IF COL_LENGTH('dbo.Promotions', 'ImageUrl') IS NULL
    ALTER TABLE dbo.Promotions ADD ImageUrl NVARCHAR(MAX) NULL;
GO
IF COL_LENGTH('dbo.PromotionProducts', 'DiscountPercentage') IS NULL
    ALTER TABLE dbo.PromotionProducts ADD DiscountPercentage DECIMAL(18,2) NULL;
GO
IF COL_LENGTH('dbo.PromotionProducts', 'RequiredQuantity') IS NULL
    ALTER TABLE dbo.PromotionProducts ADD RequiredQuantity INT NULL;
GO
IF COL_LENGTH('dbo.PromotionProducts', 'PayQuantity') IS NULL
    ALTER TABLE dbo.PromotionProducts ADD PayQuantity INT NULL;
GO
IF COL_LENGTH('dbo.ProductStores', 'RowVersion') IS NULL
    ALTER TABLE dbo.ProductStores ADD RowVersion ROWVERSION;
GO
IF COL_LENGTH('dbo.Users', 'FailedLoginAttempts') IS NULL
    ALTER TABLE dbo.Users ADD FailedLoginAttempts INT NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0;
GO
IF COL_LENGTH('dbo.Users', 'LockoutEnd') IS NULL
    ALTER TABLE dbo.Users ADD LockoutEnd DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.Products', 'Size') IS NULL
    ALTER TABLE dbo.Products ADD Size NVARCHAR(40) NULL;
GO
IF COL_LENGTH('dbo.Products', 'SizeMl') IS NULL
    ALTER TABLE dbo.Products ADD SizeMl INT NULL;
GO
IF COL_LENGTH('dbo.OrderItems', 'SubTotal') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.OrderItems'), 'SubTotal', 'IsComputed') = 0
BEGIN
    ALTER TABLE dbo.OrderItems DROP COLUMN SubTotal;
    ALTER TABLE dbo.OrderItems ADD SubTotal AS (Quantity * UnitPrice) PERSISTED;
END
GO

/* ═══════════════════════════════════════════════════════════════════════
   DATOS INICIALES (no se duplican si ya existen)
   ═══════════════════════════════════════════════════════════════════════ */

INSERT INTO dbo.Roles (Name, Description, IsActive)
SELECT v.Name, v.Description, 1
FROM (VALUES
    (N'Admin',    N'Dueño del sistema'),
    (N'Empleado', N'Trabajadores de la tienda'),
    (N'Cliente',  N'Clientes finales')
) AS v (Name, Description)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Name = v.Name);
GO

-- Sucursal principal (el código usa StoreId = 1)
IF NOT EXISTS (SELECT 1 FROM dbo.Stores)
    INSERT INTO dbo.Stores (Name, Address, Phone, WhatsApp, OpenTime, CloseTime, IsActive)
    VALUES (N'Haina', N'Sucursal Haina', '809-000-0000', '809-111-1111', '08:00', '22:00', 1);
GO

INSERT INTO dbo.Categories (Name, IsActive)
SELECT v.Name, 1
FROM (VALUES
    (N'Ron'), (N'Whisky'), (N'Vodka'), (N'Tequila'), (N'Cervezas'), (N'Vino Tinto'),
    (N'Vino Blanco'), (N'Champagne'), (N'Licores Dulces'), (N'Energizantes'), (N'Mixers'), (N'Snacks')
) AS v (Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories c WHERE c.Name = v.Name);
GO

INSERT INTO dbo.Brands (Name, IsActive)
SELECT v.Name, 1
FROM (VALUES
    (N'Bacardí'), (N'Brugal'), (N'Barceló'), (N'Captain Morgan'), (N'Johnnie Walker'),
    (N'Jack Daniel''s'), (N'Chivas Regal'), (N'Absolut'), (N'Smirnoff'), (N'Jose Cuervo'),
    (N'Corona'), (N'Heineken'), (N'Moët & Chandon'), (N'Red Bull'), (N'Coca-Cola')
) AS v (Name)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Brands b WHERE b.Name = v.Name);
GO

/* ═══════════════════════════════════════════════════════════════════════
   REVISIÓN FINAL: compara la base de datos con lo que espera el código.
   Resultado 1 = columnas que faltan  (debe salir VACÍO)
   Resultado 2 = columnas obligatorias que el código no usa (debe salir VACÍO)
   ═══════════════════════════════════════════════════════════════════════ */
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

SELECT e.Tabla, e.Columna AS ColumnaFaltante
FROM #esperado e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
       ON c.TABLE_SCHEMA = 'dbo' AND c.TABLE_NAME = e.Tabla AND c.COLUMN_NAME = e.Columna
WHERE c.COLUMN_NAME IS NULL
ORDER BY e.Tabla, e.Columna;

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

DROP TABLE #esperado;

SELECT 'Base de datos lista' AS Estado,
       (SELECT COUNT(*) FROM dbo.Categories) AS Categorias,
       (SELECT COUNT(*) FROM dbo.Brands)     AS Marcas,
       (SELECT COUNT(*) FROM dbo.Roles)      AS Roles,
       (SELECT COUNT(*) FROM dbo.Stores)     AS Sucursales;
GO
