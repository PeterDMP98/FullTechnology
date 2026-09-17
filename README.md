# FullTechnology

**Sistema de gestión de reparaciones** — aplicación de escritorio multiplataforma para la administración de un taller: órdenes de servicio, ventas, clientes, inventario, contabilidad y configuración del negocio.

Construido con **.NET 10** y **Avalonia UI**, arquitectura limpia (Domain / Application / Infrastructure) y base de datos **SQLite** local.

---

## Funcionalidades

| Módulo | Descripción |
|---|---|
| **Inicio** | Dashboard con métricas del negocio. |
| **Mantenimiento** | Órdenes de servicio: ingreso, seguimiento, diagnóstico, cobros y entrega. Búsqueda, filtros por estado, orden por columnas y paginación. |
| **Ventas** | Venta de accesorios con carrito y facturación. |
| **Clientes** | Almacén de clientes y proveedores. |
| **Inventario** | Productos, stock y costos. |
| **Historial** | Registro de todas las ventas con filtros (fechas, cliente, medio de pago) y total del periodo. |
| **Contable** | Cierre por periodo, balance y exportación a **PDF** y **Excel**. |
| **Configuración** | Nombre del negocio, moneda, tema claro/oscuro y backup / importación de la base de datos. |

### Características destacadas

- Tema **claro/oscuro** con design system propio.
- Datos en **SQLite** local, fuera de la carpeta de instalación (se conservan al desinstalar).
- Instalador **Inno Setup** self-contained para `win-x64` (no requiere .NET runtime).
- Exportación de reportes a **PDF** (QuestPDF) y **Excel** (ClosedXML).

---

## Stack tecnológico

- **.NET 10**
- **Avalonia 12.x** (tema Fluent, fuentes Inter)
- **MVVM** con CommunityToolkit.Mvvm
- **Microsoft.Data.Sqlite** / SQLitePCLRaw
- **ClosedXML** (Excel) y **QuestPDF** (PDF)
- **LiveChartsCore** (gráficas)

---

## Estructura del repositorio

```
FullTechnology/
├─ src/
│  ├─ Backend/
│  │  ├─ FULLTECHNOLOGY.Domain/          # Entidades y reglas de negocio
│  │  ├─ FULLTECHNOLOGY.Application/     # Servicios de aplicación
│  │  └─ FULLTECHNOLOGY.Infrastructure/  # Persistencia (SQLite), reportes PDF/Excel
│  └─ Frontend/
│     └─ FULLTECHNOLOGY.Presentation/    # Interfaz Avalonia (shell, vistas, ViewModels)
├─ tests/
│  ├─ FULLTECHNOLOGY.Domain.Tests/
│  ├─ FULLTECHNOLOGY.Application.Tests/
│  └─ FULLTECHNOLOGY.Presentation.Tests/
├─ scripts/          # Publicación self-contained e instalador
├─ installer/        # Configuración y salida del instalador (Inno Setup)
└─ FULLTECHNOLOGY.slnx
```

---

## Compilar y ejecutar

```bash
# Compilar toda la solución
dotnet build FULLTECHNOLOGY.slnx

# Ejecutar la aplicación
dotnet run --project src/Frontend/FULLTECHNOLOGY.Presentation

# Ejecutar los tests
dotnet test FULLTECHNOLOGY.slnx
```

### Publicación e instalador

```bash
# Publicación self-contained para Windows x64
.\scripts\build-publish.ps1 -Target win-x64

# Generar el instalador (Inno Setup)
.\scripts\build-installer.ps1
```

---

## Actualizaciones

Sección de seguimiento de cambios del proyecto. Las entradas más recientes van al inicio.

- **2026-09-17 — Instalador v3.2.** Recompilado el instalador self-contained `win-x64` (`FullTechnology_Setup_v3.2.exe`) con los cambios de UI; actualizados `scripts/installer.iss`, `scripts/build-installer.ps1` y changelogs.
- **2026-09-17 — Publicación inicial en GitHub.** Repositorio público con la estructura completa: frontend Avalonia, backend en capas (Domain/Application/Infrastructure), pruebas y scripts de publicación/instalador.
- **2026-09-17 — UI: iconos por módulo.** El sidebar, el avatar de usuario del topbar y las tarjetas de mantenimiento ahora usan emojis acordes a cada sección (e.g. 🛒 Ventas, 👤 Clientes, 📦 Inventario); los íconos del sidebar son más grandes. El botón de maximizar/restaurar del título cambia de glifo según el estado de la ventana.