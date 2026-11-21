# Base de datos — Daniel Liquor Store

Cinco scripts, numerados. Cada uno hace una sola cosa.

| Script | Para qué sirve | Cambia datos |
|---|---|---|
| `01-crear-base-de-datos.sql` | Crea la base completa (tablas, relaciones, datos iniciales) o la pone al día si ya existe | Solo agrega lo que falte |
| `02-borrar-base-de-datos.sql` | Borra la base de datos entera. Pide confirmar cambiando `@Confirmar` a `'SI'` | Borra todo |
| `03-verificar-esquema.sql` | Compara la base contra lo que espera el código. Útil ante un error "Invalid column name" | No |
| `04-auditoria-consultas.sql` | Quién hizo qué y cómo están las cuentas por rol | No |
| `05-consultas-utiles.sql` | Usuarios, bloqueos, catálogo con tamaños, stock, filas por tabla | No |

## Orden de uso

- **Base nueva o actualizar la que ya tienes:** `01`.
- **Empezar de cero:** `02` (con `@Confirmar = 'SI'`) y luego `01`.
- **Después de actualizar el proyecto:** `03` y revisa que los resultados 1 y 2 salgan vacíos.

## Scripts viejos que ya no hacen falta

| Script viejo | Dónde quedó |
|---|---|
| Querry para crear la db.sql | `01` |
| AddStockRowVersion.sql | `01` (columna `RowVersion` de `ProductStores`); la consulta de revisión, en `05` |
| query de verificacion de intentos de adminsion.sql | `01` (columnas `FailedLoginAttempts` y `LockoutEnd`); la consulta, en `05` |
| 1-agregar-tamanos-productos.sql | `01` (columnas `Size` y `SizeMl`); las consultas de revisión, en `05` |
| Adaptacion de userAdminAuditoria.sql | `04` |
| Query para borrar Db.sql | `02` |
