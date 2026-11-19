# MARKERP

Sistema de gestión (ERP) para tiendas y emprendimientos: ventas, inventario, proveedores, cotizaciones, compras, clientes, talento humano y reportes, con una interfaz minimalista en tonos pastel.

Proyecto académico · Ingeniería de Software · Universidad de Cundinamarca.

## Tecnologías

| Capa | Herramienta |
|---|---|
| Lenguaje | C# (.NET 8) |
| Framework web | ASP.NET Core MVC con vistas Razor |
| Base de datos | Entity Framework Core · SQLite en local · PostgreSQL en Render |
| Interfaz | Bootstrap 5, Bootstrap Icons, Chart.js, CSS propio con 5 temas pastel |
| Seguridad | Autenticación con cookies, contraseñas cifradas (PasswordHasher) y roles |
| Pruebas | xUnit |
| Despliegue | Docker en Render, con `render.yaml` |

## Módulos

1. **Inicio:** indicadores del día y del mes, gráfica de ventas, stock bajo, últimas ventas y novedades del equipo.
2. **Ventas:** punto de venta con buscador, descuentos e IVA, factura imprimible o en PDF, y anulación que devuelve los productos al inventario.
3. **Inventario:** productos, categorías, bodegas, kardex de movimientos (entradas, salidas y conteo físico) y alertas de stock mínimo.
4. **Clientes:** ficha de cada cliente, historial de compras y etiqueta de cliente frecuente.
5. **Proveedores:** ficha con calificación, condiciones y tiempo de entrega. Incluye la consulta **"¿Quién vende este producto?"** con precios de cada proveedor.
6. **Cotizaciones:**
   - Solicitud a varios proveedores, con envío simulado y PDF para cada uno.
   - Registro de las respuestas.
   - **Comparativo con puntaje**: 50 % precio, 30 % tiempo de entrega y 20 % calificación del proveedor.
   - La más conveniente se marca como **recomendada**.
   - Eliges con cuál te quedas, con un motivo, y se calcula el ahorro.
   - Se **genera la orden de compra**.
7. **Compras:** órdenes de compra (borrador, enviada, recibida o cancelada). Al recibir, se suma al inventario y se actualizan los precios.
8. **Talento humano:**
   - Ficha completa del empleado:
     - Documento, fecha de nacimiento, **edad calculada**, sexo, estado civil y personas a cargo.
     - **Estatura, peso**, IMC, **tipo de sangre (RH)** y tallas de dotación.
     - **EPS, ARL, fondo de pensiones, fondo de cesantías y caja de compensación**.
     - Nivel educativo, cargo, área, contrato, jornada, salario, **antigüedad** y cuenta bancaria.
     - Contacto de emergencia.
   - Turnos semanales y novedades (incapacidades, vacaciones, permisos), con exportación a CSV.
9. **Reportes:** ventas por día, por método de pago y por vendedor; productos más vendidos; utilidad; valor del inventario; compras por proveedor; ahorro por cotizaciones; personal por área y por EPS; nómina. Exportación a CSV y PDF.
10. **Configuración:** datos del negocio, IVA, tema por defecto, usuarios y roles, y bitácora de actividad.

## Usuarios de demostración

| Rol | Correo | Contraseña | Acceso |
|---|---|---|---|
| Administrador | admin@markerp.com | Admin123* | Todo |
| Vendedor | ventas@markerp.com | Ventas123* | Ventas, clientes, consulta de inventario |
| Encargado de inventario | inventario@markerp.com | Inventario123* | Inventario, proveedores, cotizaciones, compras |
| Talento humano | rrhh@markerp.com | Rrhh123* | Empleados, turnos, novedades |

En la pantalla de inicio de sesión hay botones que llenan estos datos.

> **Importante:** cambia estas contraseñas desde **Configuración → Usuarios** antes de compartir el enlace público.

## Ejecutar en VS Code







## Estructura

```
MarkERP/
├─ MarkERP.sln
├─ Dockerfile · render.yaml · .gitignore
├─ src/MarkERP.Web/
│  ├─ Program.cs              → configuración, base de datos, autenticación
│  ├─ Models/                 → entidades (Producto, Venta, Proveedor, Cotizacion, Empleado…)
│  ├─ Data/                   → AppDbContext y datos de demostración (SeedData)
│  ├─ Services/               → inventario, bitácora, evaluador de cotizaciones
│  ├─ Controllers/            → un controlador por módulo
│  ├─ ViewModels/             → modelos de formularios y reportes
│  ├─ Views/                  → vistas Razor (.cshtml)
│  └─ wwwroot/css · js        → estilos pastel, temas y scripts
└─ tests/MarkERP.Tests/       → pruebas unitarias xUnit
```

## Decisiones de diseño

- **Envío de cotizaciones y órdenes:** es *simulado*. Queda registrado con fecha y correo destino. Para enviar correos reales se puede integrar SMTP más adelante.
- **PDF:** facturas, solicitudes de cotización y órdenes de compra tienen una vista imprimible. El botón "Imprimir o guardar como PDF" usa el navegador, así que no hace falta ninguna librería extra.
- **"Simular respuestas":** este botón, en el detalle de una cotización, genera respuestas de los proveedores con precios cercanos a su precio de referencia. Sirve para hacer la demo en clase.
- **Temas:**
  - Hay 5 temas: Algodón (multicolor), Cielo, Lavanda, Menta y Durazno.
  - Cada usuario lo cambia con los círculos de la barra superior y su elección queda guardada en el navegador.
  - Cada módulo tiene su propio color pastel.
- La factura de venta es interna y **no reemplaza la factura electrónica de la DIAN**.
