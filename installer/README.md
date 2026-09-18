# FullTechnology — Instalador

Instalador oficial de **FullTechnology** (`FullTechnology_Setup_v3.5.exe`).

## Características del instalador

- **Versión 3.5** generada con **Inno Setup 6** (scripts en `../scripts/installer.iss`).
- Instala la **publicación self-contained** de Avalonia para `win-x64` (no requiere .NET runtime ni conexión a internet): toda la carpeta `publish` se copia al directorio de instalación.
- Mismo **AppId** en todas las versiones → **actualiza en sitio** sin dejar instalaciones huérfanas.
- Página del instalador para el **nombre del negocio**: se escribe en `%LOCALAPPDATA%\DecoTechnology\settings.ini` y la app lo muestra en el shell.
- **Actualiza la base de datos en sitio**: al arrancar la app aplica las migraciones del esquema sobre bases existentes (v2/v3), conservando los datos.
- **No borra la base de datos al desinstalar**: los datos viven en `%LOCALAPPDATA%\DecoTechnology\DecoTechnology.db` fuera de la carpeta de programa.
- Windows 10/11 x64.

## Qué incluye la aplicación

- **Inicio**: tablero de alertas — stock bajo de accesorios/repuestos y órdenes con tiempo vencido (recibido→listo, listo→entregado), con umbrales editables en Configuración.
- **Mantenimiento**: órdenes de reparación con búsqueda, filtros, orden por columnas y paginación.
- **Ventas**: alta de ventas de accesorios con carrito y **facturación en PDF** (previsualización + impresión con selección de impresora).
- **Historial**: registro de todas las ventas con filtros y **reimpresión de la factura** desde el menú ⋯ de cada fila.
- **Inventario**: productos, stock y costos, con **exportar a PDF/Excel** e **importación** desde Excel/CSV.
- **Contabilidad**: resumen contable del periodo (Ventas/Cobros), filtros por vista y medio, exportación a **PDF** y **Excel**.
- **Configuración**: nombre del negocio y moneda, tema claro/oscuro, backup / importación de la base de datos y **umbrales de las alertas**.
- **Tablas redimensionables**: ajusta el ancho de cualquier columna arrastrando su borde, como Excel.

Para reproducir el instalador desde el código: `scripts/build-publish.ps1 -Target win-x64`, luego `scripts/build-installer.ps1`.
El historial de versiones está en `CHANGELOG.md` y la estructura del repositorio en `docs/11-MIGRACION.md`.