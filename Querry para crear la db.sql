/* ============================================================
   SCRIPT DE RECREACIÓN COMPLETA
   BASE DE DATOS: DanielLicorsStoreDB
   Sistema Web de Ventas + Administración
   Mobile First + WhatsApp Orders
   ============================================================ */

-- Crear base de datos si no existe
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'DanielLicorsStoreDB')
BEGIN
    CREATE DATABASE DanielLicorsStoreDB;
END
GO

USE DanielLicorsStoreDB;
GO

/* ============================================================
   TABLAS BASE DE CATÁLOGO
   ============================================================ */

-- Categorías de productos (Ron, Whisky, Vodka, etc.)
CREATE TABLE Categories (
    CategoryId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Marcas de bebidas
CREATE TABLE Brands (
    BrandId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

-- Productos
CREATE TABLE Products (
    ProductId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    BrandId INT NULL,
    CategoryId INT NOT NULL,
    IsPremium BIT NOT NULL DEFAULT 0,
    ImageUrl NVARCHAR(500),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    AgingYears INT NULL,

    CONSTRAINT FK_Product_Category FOREIGN KEY (CategoryId)
        REFERENCES Categories(CategoryId),

    CONSTRAINT FK_Product_Brand FOREIGN KEY (BrandId)
        REFERENCES Brands(BrandId)
);

/* ============================================================
   SUCURSALES
   ============================================================ */

CREATE TABLE Stores (
    StoreId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Address NVARCHAR(250) NOT NULL,
    Phone NVARCHAR(20),
    WhatsApp NVARCHAR(20),
    OpenTime TIME,
    CloseTime TIME,
    IsActive BIT NOT NULL DEFAULT 1
);

/* ============================================================
   PRODUCTO POR SUCURSAL (PRECIO + STOCK)
   ============================================================ */

CREATE TABLE ProductStores (
    ProductStoreId INT IDENTITY PRIMARY KEY,
    ProductId INT NOT NULL,
    StoreId INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    Stock INT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_ProductStore_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId),

    CONSTRAINT FK_ProductStore_Store FOREIGN KEY (StoreId)
        REFERENCES Stores(StoreId),

    CONSTRAINT UQ_Product_Store UNIQUE (ProductId, StoreId)
);

/* ============================================================
   PRODUCTOS MÁS VENDIDOS
   ============================================================ */

CREATE TABLE BestSellers (
    BestSellerId INT IDENTITY PRIMARY KEY,
    ProductId INT NOT NULL,
    OrderPriority INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,

    CONSTRAINT FK_BestSeller_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

/* ============================================================
   PROMOCIONES Y COMBOS
   ============================================================ */

-- Promociones generales
CREATE TABLE Promotions (
    PromotionId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    PromotionType NVARCHAR(50), -- Weekly, Combo, Highlight
    IsActive BIT NOT NULL DEFAULT 1
);

-- Productos dentro de promociones
CREATE TABLE PromotionProducts (
    PromotionProductId INT IDENTITY PRIMARY KEY,
    PromotionId INT NOT NULL,
    ProductId INT NOT NULL,
    SpecialPrice DECIMAL(10,2) NULL,

    CONSTRAINT FK_PromotionProduct_Promotion FOREIGN KEY (PromotionId)
        REFERENCES Promotions(PromotionId),

    CONSTRAINT FK_PromotionProduct_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

/* ============================================================
   COMBOS
   ============================================================ */

CREATE TABLE Combos (
    ComboId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    ComboPrice DECIMAL(10,2) NOT NULL,
    StartDate DATETIME,
    EndDate DATETIME,
    ImageUrl NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE ComboProducts (
    ComboProductId INT IDENTITY PRIMARY KEY,
    ComboId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,

    CONSTRAINT FK_ComboProduct_Combo FOREIGN KEY (ComboId)
        REFERENCES Combos(ComboId),

    CONSTRAINT FK_ComboProduct_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

/* ============================================================
   USUARIOS
   ============================================================ */

CREATE TABLE Users (
    UserId NVARCHAR(450) PRIMARY KEY,
    FullName NVARCHAR(150),
    LastName NVARCHAR(150) NULL,
    Email NVARCHAR(150) NULL,
    Phone NVARCHAR(20),
    Age INT NULL,
    PasswordHash NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

/* ============================================================
   ROLES
   ============================================================ */

CREATE TABLE Roles (
    RoleId INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(200),
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE UserRoles (
    UserRoleId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    RoleId INT NOT NULL,
    AssignedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId),

    CONSTRAINT UQ_User_Role UNIQUE (UserId, RoleId)
);

/* ============================================================
   CARRITO DE COMPRAS (PERSISTENTE)
   ============================================================ */

CREATE TABLE Carts (
    CartId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NULL,
    SessionId NVARCHAR(100) NULL,
    StoreId INT NOT NULL,
    CartStatus NVARCHAR(20) NOT NULL DEFAULT 'Active', -- Active, Sent, Abandoned
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Cart_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Cart_Store FOREIGN KEY (StoreId)
        REFERENCES Stores(StoreId)
);

-- Productos dentro del carrito
CREATE TABLE CartItems (
    CartItemId INT IDENTITY PRIMARY KEY,
    CartId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    SubTotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_CartItem_Cart FOREIGN KEY (CartId)
        REFERENCES Carts(CartId),

    CONSTRAINT FK_CartItem_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

-- Combos dentro del carrito
CREATE TABLE CartCombos (
    CartComboId INT IDENTITY PRIMARY KEY,
    CartId INT NOT NULL,
    ComboId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    SubTotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_CartCombo_Cart FOREIGN KEY (CartId)
        REFERENCES Carts(CartId),

    CONSTRAINT FK_CartCombo_Combo FOREIGN KEY (ComboId)
        REFERENCES Combos(ComboId)
);

/* ============================================================
   PEDIDOS
   ============================================================ */

CREATE TABLE Orders (
    OrderId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NULL,
    StoreId INT NOT NULL,
    OrderStatus NVARCHAR(30) NOT NULL, -- Pending, InProcess, Completed
    PaymentMethod NVARCHAR(20),
    TotalAmount DECIMAL(10,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Order_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId),

    CONSTRAINT FK_Order_Store FOREIGN KEY (StoreId)
        REFERENCES Stores(StoreId)
);

CREATE TABLE OrderItems (
    OrderItemId INT IDENTITY PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    SubTotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_OrderItem_Order FOREIGN KEY (OrderId)
        REFERENCES Orders(OrderId),

    CONSTRAINT FK_OrderItem_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId)
);

/* ============================================================
   MOVIMIENTOS DE STOCK (AUDITORÍA)
   ============================================================ */

CREATE TABLE StockMovements (
    StockMovementId INT IDENTITY PRIMARY KEY,
    ProductId INT NOT NULL,
    StoreId INT NOT NULL,
    Quantity INT NOT NULL,
    MovementType NVARCHAR(20) NOT NULL, -- In, Out, Adjustment
    UserId NVARCHAR(450) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_StockMovement_Product FOREIGN KEY (ProductId)
        REFERENCES Products(ProductId),

    CONSTRAINT FK_StockMovement_Store FOREIGN KEY (StoreId)
        REFERENCES Stores(StoreId),

    CONSTRAINT FK_StockMovement_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);

/* ============================================================
   CONTENIDO DEL SITIO
   ============================================================ */

CREATE TABLE Events (
    EventId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500),
    ImageUrl NVARCHAR(500),
    StartDate DATETIME,
    EndDate DATETIME,
    IsActive BIT NOT NULL DEFAULT 1
);

CREATE TABLE Testimonials (
    TestimonialId INT IDENTITY PRIMARY KEY,
    CustomerName NVARCHAR(150),
    Comment NVARCHAR(500),
    Rating INT,
    IsActive BIT NOT NULL DEFAULT 1
);

/* ============================================================
   CONFIGURACIÓN Y LEGAL
   ============================================================ */

CREATE TABLE SiteConfigs (
    SiteConfigId INT IDENTITY PRIMARY KEY,
    ConfigKey NVARCHAR(100) NOT NULL,
    ConfigValue NVARCHAR(MAX) NOT NULL
);

/* ============================================================
   AUDITORÍA
   ============================================================ */

CREATE TABLE AuditLogs (
    AuditLogId INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450),
    Action NVARCHAR(200),
    Entity NVARCHAR(100),
    EntityId NVARCHAR(100),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_AuditLog_User FOREIGN KEY (UserId)
        REFERENCES Users(UserId)
);

/* ============================================================
   DATOS INICIALES (SEED)
   ============================================================ */

-- Roles
INSERT INTO Roles (Name, Description, IsActive) VALUES
('Admin',    'Dueño del sistema',          1),
('Empleado', 'Trabajadores de la tienda',  1),
('Cliente',  'Clientes finales',           1);


-- Tienda
INSERT INTO Stores (Name, Address, Phone, WhatsApp, OpenTime, CloseTime, IsActive)
VALUES ('Haina', 'Sucursal Haina', '809-000-0000', '809-111-1111', '08:00', '22:00', 1);

-- Verifica que quedó con StoreId = 1
SELECT StoreId, Name FROM Stores;

-- Categorías
INSERT INTO Categories (Name, IsActive) VALUES
('Ron', 1),
('Whisky', 1),
('Vodka', 1),
('Tequila', 1),
('Cervezas', 1),
('Vino Tinto', 1),
('Vino Blanco', 1),
('Champagne', 1),
('Licores Dulces', 1),
('Energizantes', 1),
('Mixers', 1),
('Snacks', 1);

-- Marcas
INSERT INTO Brands (Name, IsActive) VALUES
('Bacardí', 1),
('Brugal', 1),
('Barceló', 1),
('Captain Morgan', 1),
('Johnnie Walker', 1),
('Jack Daniel''s', 1),
('Chivas Regal', 1),
('Absolut', 1),
('Smirnoff', 1),
('Jose Cuervo', 1),
('Corona', 1),
('Heineken', 1),
('Moët & Chandon', 1),
('Red Bull', 1),
('Coca-Cola', 1);

-- ================= FIN DEL SCRIPT =================

SELECT * FROM UserRoles

SELECT * FROM Users

SELECT
    u.UserId,
    u.FullName,
    u.LastName,
    u.Email,
    u.Phone,
    u.IsActive AS UserIsActive,
    r.RoleId,
    r.Name AS RoleName,
    r.Description AS RoleDescription,
    ur.AssignedAt
FROM Users u
INNER JOIN UserRoles ur ON u.UserId = ur.UserId
INNER JOIN Roles r ON ur.RoleId = r.RoleId;