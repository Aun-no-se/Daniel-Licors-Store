/* ============================================================
   SCRIPT DE ACTUALIZACIÓN - PROMOCIONES
   DanielLicorsStoreDB
   Ejecutar en orden sobre la BD existente
   ============================================================ */

USE DanielLicorsStoreDB;
GO

-- ============================================================
-- 1. Agregar columnas nuevas a PromotionProducts
--    (si ya las tiene del ALTER anterior, el IF evita error)
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PromotionProducts' AND COLUMN_NAME = 'PromotionalPrice'
)
BEGIN
    ALTER TABLE PromotionProducts
    ADD PromotionalPrice   DECIMAL(10,2) NULL,
        DiscountPercentage DECIMAL(5,2)  NULL,
        RequiredQuantity   INT           NULL,
        PayQuantity        INT           NULL;
    PRINT 'Columnas agregadas a PromotionProducts';
END
ELSE
    PRINT 'Columnas ya existían en PromotionProducts';
GO

-- ============================================================
-- 2. Agregar ImageUrl a Promotions
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Promotions' AND COLUMN_NAME = 'ImageUrl'
)
BEGIN
    ALTER TABLE Promotions
    ADD ImageUrl NVARCHAR(500) NULL;
    PRINT 'ImageUrl agregado a Promotions';
END
ELSE
    PRINT 'ImageUrl ya existía en Promotions';
GO

-- ============================================================
-- 3. Verificar resultado
-- ============================================================

SELECT
    c.COLUMN_NAME,
    c.DATA_TYPE,
    c.IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME IN ('Promotions', 'PromotionProducts')
ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;
GO

PRINT '✅ Script completado correctamente.';
