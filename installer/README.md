# FullTechnology — Instalador

Instalador oficial de **FullTechnology** (`FullTechnology_Setup_v3.1.exe`).

## Características del instalador

- **Versión 3.1** generada con **Inno Setup 6** (scripts en `../scripts/installer.iss`).
- Instala la **publicación self-contained** de Avalonia para `win-x64` (no requiere .NET runtime ni conexión a internet): toda la carpeta `publish` se copia al directorio de instalación.
- Mismo **AppId** en todas las versiones → **actualiza en sitio** sin dejar instalaciones huérfanas.
- Página del instalador para el **nombre del negocio**: se escribe en `%LOCALAPPDATA%\DecoTechnology\settings.ini` y la app lo muestra en el shell.
- **No borra la base de datos al desinstalar**: los datos viven en `%LOCALAPPDATA%\DecoTechnology\DecoTechnology.db` fuera de la carpeta de programa.
- Windows 10/11 x64.

## Qué incluye la aplicación

- **Ventas**: alta de ventas de accesorios con carrito y facturación.
- **Historial**: registro de todas las ventas con filtros (fechas, cliente, medio de pago) y total del periodo.
- **Contabilidad**: resumen contable del periodo (Ventas/Cobros), filtros por vista y medio, exportación a **PDF** y **Excel**.
- **Configuración**: nombre del negocio y moneda (se propaga al shell), tema claro/oscuro, y **backup / importación** de la base de datos.

Para reproducir el instalador desde el código: `scripts/build-publish.ps1 -Target win-x64`, luego `scripts/build-installer.ps1`.
El historial de versiones está en `CHANGELOG.md` y la estructura del repositorio en `docs/11-MIGRACION.md`.