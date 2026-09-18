# CHANGELOG del instalador

Registro de versiones del instalador de FullTechnology.

## v3.5 — 2026-09-18

- **Todo el software v3.5 (spec `docs/specs/actulizacion v3.5.md`)**: Inicio como tablero de alertas (stock bajo accesorios/repuestos y órdenes con tiempo vencido), umbrales editables en Configuración, **factura PDF** con previsualización e impresión al cerrar una venta (reimprimible desde Historial), exportar/importar del inventario (PDF/Excel), formularios alineados a la izquierda, corrección `(null) to System.Decimal`, columnas alineadas y **redimensionables** en todas las tablas.
- **Corrección de migración de base de datos**: al arrancar la app ahora aplica las migraciones del esquema v3.5 (`StatusChangedAt` y columnas de alertas) sobre bases existentes v2/v3; se reparó también el arranque desde Visual Studio (perfil de depuración `.NET Core` vía `launchSettings.json`, limpieza de builds viejos `net10.0`).
- **TFM `net10.0-windows`**: el publish self-contained se genera ahora en `bin\Release\net10.0-windows\win-x64\publish\` (actualizado en `scripts/installer.iss`).
- Recompilado y publicado como instalador self-contained `win-x64` (`FullTechnology_Setup_v3.5.exe`).

## v3.2 — 2026-09-17

- **Actualización de interfaz**: iconos por módulo en el sidebar, el avatar de usuario del topbar y las tarjetas de mantenimiento, con íconos más grandes.
- **Botón maximizar/restaurar dinámico** en el titlebar: cambia de glifo según el estado de la ventana.
- Recompilado y publicado como instalador self-contained `win-x64` (`FullTechnology_Setup_v3.2.exe`).

## v3.1 — 2026-09-14

- **Migración a Avalonia (F3–F13)**: el instalador instala la publicación **self-contained** (`net10.0`, `win-x64`, ~247 MB) en lugar del antiguo ZIP/EJECUTABLE WinForms.
- Mismo **AppId `{DECO-TECH-2026-0001}`** que la v2 → la actualización **se instala en sitio** sin desinstalar antes y sin dejar huérfanos.
- Página de **nombre del negocio** que escribe `%LOCALAPPDATA%\DecoTechnology\settings.ini`.
- **No elimina la base de datos** (`%LOCALAPPDATA%\DecoTechnology\DecoTechnology.db`) al desinstalar.
- **ERR-014 cerrado**: los scripts apuntaban a la salida WinForms; ahora `scripts/build-publish.ps1 -Target win-x64` + `scripts/build-installer.ps1` generan este instalador de forma reproducible.
- Multiplataforma: el instalador Windows queda cubierto por esta v3.1; Linux/macOS se entregan como publicación self-contained (ver `docs/11-MIGRACION.md`, todos los nativos por RID).

## v2.x — WinForms (retirada)

- Instalador de la versión 2 (WinForms/.NET Framework) usado hasta F12.
- **Retirada en v3.1**: el ejecutable WinForms fue eliminado del proyecto (`FullTechnology/` v2 borrada en la reorganización, 2026-09-15). Se conserva aquí solo como referencia histórica.