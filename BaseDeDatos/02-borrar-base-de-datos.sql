/* ═══════════════════════════════════════════════════════════════════════
   02 · BORRAR LA BASE DE DATOS  —  Daniel Liquor Store

   ⚠️  BORRA TODO: usuarios, bebidas, stock, combos, promociones, pedidos y
       carritos. No se puede deshacer. Haz antes una copia de seguridad si
       tienes datos que quieras conservar.

   Seguridad: por defecto NO borra nada. Para borrar, cambia @Confirmar a 'SI'.
   Borra la base de datos completa (todas las tablas, relaciones y datos), así
   que no hay que preocuparse por el orden de las tablas.

   Para borrar la base de PRUEBA, cambia @NombreBD a N'DanielLicorsStoreDB_Prueba'.
   Después de borrar, para empezar de cero corre: 01-crear-base-de-datos.sql
   ═══════════════════════════════════════════════════════════════════════ */
USE master;
GO

DECLARE @NombreBD  SYSNAME     = N'DanielLicorsStoreDB';   -- base que se va a borrar
DECLARE @Confirmar NVARCHAR(2) = N'NO';                     -- ← cámbialo a 'SI' para borrar

IF @Confirmar <> N'SI'
BEGIN
    PRINT N'Seguridad: no se borró nada. Cambia @Confirmar a ''SI'' para borrar [' + @NombreBD + N'].';
    RETURN;
END

IF DB_ID(@NombreBD) IS NULL
BEGIN
    PRINT N'La base [' + @NombreBD + N'] no existe. No se hizo nada.';
    RETURN;
END

DECLARE @sql NVARCHAR(MAX) =
      N'ALTER DATABASE ' + QUOTENAME(@NombreBD) + N' SET SINGLE_USER WITH ROLLBACK IMMEDIATE; '   -- cierra conexiones abiertas
    + N'DROP DATABASE '  + QUOTENAME(@NombreBD) + N';';
EXEC (@sql);

PRINT N'Base [' + @NombreBD + N'] borrada.';
GO

/* Comprobación: las bases que quedan (no debe aparecer la que borraste) */
SELECT name AS BaseDeDatos FROM sys.databases WHERE name LIKE N'DanielLicors%' ORDER BY name;
GO
