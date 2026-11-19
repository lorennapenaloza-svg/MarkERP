using MarkERP.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ConfiguracionNegocio> Configuracion => Set<ConfiguracionNegocio>();
    public DbSet<Bitacora> Bitacora => Set<Bitacora>();

    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Bodega> Bodegas => Set<Bodega>();
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<MovimientoInventario> Movimientos => Set<MovimientoInventario>();

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Venta> Ventas => Set<Venta>();
    public DbSet<DetalleVenta> DetallesVenta => Set<DetalleVenta>();

    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<ProveedorProducto> ProveedorProductos => Set<ProveedorProducto>();
    public DbSet<SolicitudCotizacion> Solicitudes => Set<SolicitudCotizacion>();
    public DbSet<ItemSolicitud> ItemsSolicitud => Set<ItemSolicitud>();
    public DbSet<EnvioCotizacion> EnviosCotizacion => Set<EnvioCotizacion>();
    public DbSet<Cotizacion> Cotizaciones => Set<Cotizacion>();
    public DbSet<DetalleCotizacion> DetallesCotizacion => Set<DetalleCotizacion>();
    public DbSet<OrdenCompra> OrdenesCompra => Set<OrdenCompra>();
    public DbSet<DetalleOrdenCompra> DetallesOrdenCompra => Set<DetalleOrdenCompra>();

    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<Novedad> Novedades => Set<Novedad>();
}
