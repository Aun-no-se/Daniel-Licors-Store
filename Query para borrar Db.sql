
USE master;
GO

-- Mata todas las conexiones activas a la base de datos
ALTER DATABASE DanielLicorsStoreDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO

/* ============================================================
   SCRIPT DE BORRADO TOTAL
   BASE DE DATOS: DanielLicorsStoreDB + ASP.NET Identity
   ⚠️  CORRE ESTO PRIMERO, LUEGO EL SCRIPT DE RECREACIÓN
   ============================================================ */

USE DanielLicorsStoreDB;
GO

/* ============================================================
   PASO 1 — DESACTIVAR TODAS LAS FOREIGN KEYS
   (evita errores de dependencia al borrar)
   ============================================================ */
EXEC sp_MSForEachTable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL';
GO

/* ============================================================
   PASO 2 — BORRAR TABLAS DE ASP.NET IDENTITY
   ============================================================ */
IF OBJECT_ID('AspNetUserRoles',   'U') IS NOT NULL DROP TABLE AspNetUserRoles;
IF OBJECT_ID('AspNetUserClaims',  'U') IS NOT NULL DROP TABLE AspNetUserClaims;
IF OBJECT_ID('AspNetUserLogins',  'U') IS NOT NULL DROP TABLE AspNetUserLogins;
IF OBJECT_ID('AspNetUserTokens',  'U') IS NOT NULL DROP TABLE AspNetUserTokens;
IF OBJECT_ID('AspNetRoleClaims',  'U') IS NOT NULL DROP TABLE AspNetRoleClaims;
IF OBJECT_ID('AspNetUsers',       'U') IS NOT NULL DROP TABLE AspNetUsers;
IF OBJECT_ID('AspNetRoles',       'U') IS NOT NULL DROP TABLE AspNetRoles;
GO

/* ============================================================
   PASO 3 — BORRAR TABLAS DE AUDITORÍA Y STOCK
   ============================================================ */
IF OBJECT_ID('AuditLogs',       'U') IS NOT NULL DROP TABLE AuditLogs;
IF OBJECT_ID('StockMovements',  'U') IS NOT NULL DROP TABLE StockMovements;
GO

/* ============================================================
   PASO 4 — BORRAR TABLAS DE CARRITO
   ============================================================ */
IF OBJECT_ID('CartItems',  'U') IS NOT NULL DROP TABLE CartItems;
IF OBJECT_ID('CartCombos', 'U') IS NOT NULL DROP TABLE CartCombos;
IF OBJECT_ID('Carts',      'U') IS NOT NULL DROP TABLE Carts;
GO

/* ============================================================
   PASO 5 — BORRAR TABLAS DE PEDIDOS
   ============================================================ */
IF OBJECT_ID('OrderItems', 'U') IS NOT NULL DROP TABLE OrderItems;
IF OBJECT_ID('Orders',     'U') IS NOT NULL DROP TABLE Orders;
GO

/* ============================================================
   PASO 6 — BORRAR TABLAS DE COMBOS Y PROMOCIONES
   ============================================================ */
IF OBJECT_ID('ComboProducts',      'U') IS NOT NULL DROP TABLE ComboProducts;
IF OBJECT_ID('Combos',             'U') IS NOT NULL DROP TABLE Combos;
IF OBJECT_ID('PromotionProducts',  'U') IS NOT NULL DROP TABLE PromotionProducts;
IF OBJECT_ID('Promotions',         'U') IS NOT NULL DROP TABLE Promotions;
GO

/* ============================================================
   PASO 7 — BORRAR TABLAS DE INVENTARIO
   ============================================================ */
IF OBJECT_ID('ProductStores', 'U') IS NOT NULL DROP TABLE ProductStores;
IF OBJECT_ID('BestSellers',   'U') IS NOT NULL DROP TABLE BestSellers;
GO

/* ============================================================
   PASO 8 — BORRAR TABLAS DE PRODUCTOS Y CATÁLOGO
   ============================================================ */
IF OBJECT_ID('Products',   'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Brands',     'U') IS NOT NULL DROP TABLE Brands;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Stores',     'U') IS NOT NULL DROP TABLE Stores;
GO

/* ============================================================
   PASO 9 — BORRAR TABLAS DE USUARIOS Y ROLES
   ============================================================ */
IF OBJECT_ID('UserRoles', 'U') IS NOT NULL DROP TABLE UserRoles;
IF OBJECT_ID('Roles',     'U') IS NOT NULL DROP TABLE Roles;
IF OBJECT_ID('Users',     'U') IS NOT NULL DROP TABLE Users;
GO

/* ============================================================
   PASO 10 — BORRAR TABLAS DE CONTENIDO Y CONFIGURACIÓN
   ============================================================ */
IF OBJECT_ID('Events',       'U') IS NOT NULL DROP TABLE Events;
IF OBJECT_ID('Testimonials', 'U') IS NOT NULL DROP TABLE Testimonials;
IF OBJECT_ID('SiteConfigs',  'U') IS NOT NULL DROP TABLE SiteConfigs;
GO

/* ============================================================
   VERIFICACIÓN FINAL
   (debe devolver 0 tablas si todo se borró bien)
   ============================================================ */
SELECT 
    COUNT(*) AS TablasSobrantes,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Base de datos limpia. Ya puedes correr el script de recreación.'
        ELSE '⚠️  Todavía quedan tablas. Revisa arriba.'
    END AS Estado
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE';
GO

-- ================= FIN DEL SCRIPT =================
-- Ahora corre: RecreateDB_DanielLicorsStoreDB.sql

USE master;
GO
ALTER DATABASE DanielLicorsStoreDB SET MULTI_USER;
GO