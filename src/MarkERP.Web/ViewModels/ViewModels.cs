using System.ComponentModel.DataAnnotations;
using MarkERP.Web.Models;

namespace MarkERP.Web.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage = "Escribe tu correo"), EmailAddress(ErrorMessage = "Correo no válido")] public string Email { get; set; } = "";
    [Required(ErrorMessage = "Escribe tu contraseña"), DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}

public class DashboardVM
{
    public decimal VentasHoy { get; set; }
    public int NumVentasHoy { get; set; }
    public decimal VentasMes { get; set; }
    public decimal VentasMesAnterior { get; set; }
    public int TotalProductos { get; set; }
    public decimal ValorInventario { get; set; }
    public List<Producto> StockBajo { get; set; } = new();
    public int CotizacionesAbiertas { get; set; }
    public int OrdenesPendientes { get; set; }
    public int EmpleadosActivos { get; set; }
    public int EmpleadosEnTurno { get; set; }
    public List<Venta> UltimasVentas { get; set; } = new();
    public List<Novedad> NovedadesActivas { get; set; } = new();
    public List<string> EtiquetasDias { get; set; } = new();
    public List<decimal> VentasDias { get; set; } = new();
    public List<(string Nombre, int Cantidad)> TopProductos { get; set; } = new();
}

public class LineaVM
{
    public int ProductoId { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class NuevaVentaVM
{
    public int? ClienteId { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    [Range(0, 100)] public decimal PorcentajeDescuento { get; set; }
    public List<LineaVM> Items { get; set; } = new();
}

public class MovimientoVM
{
    public int ProductoId { get; set; }
    public string Tipo { get; set; } = TiposMovimiento.Entrada;
    [Range(0, 1000000)] public int Cantidad { get; set; }
    [Required(ErrorMessage = "Indica el motivo")] public string Motivo { get; set; } = "";
    public string? Referencia { get; set; }
}

public class NuevaSolicitudVM
{
    [Required(ErrorMessage = "Describe qué necesitas cotizar")] public string Descripcion { get; set; } = "";
    [DataType(DataType.Date)] public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(5);
    public List<LineaVM> Items { get; set; } = new();
    public List<int> ProveedoresIds { get; set; } = new();
}

public class RespuestaCotizacionVM
{
    public int SolicitudId { get; set; }
    public int ProveedorId { get; set; }
    [Range(0, 365)] public int TiempoEntregaDias { get; set; } = 3;
    public string CondicionesPago { get; set; } = "Contado";
    [Range(1, 365)] public int ValidezDias { get; set; } = 15;
    public string? Observaciones { get; set; }
    public List<LineaVM> Items { get; set; } = new();
}

public class NuevaOrdenVM
{
    public int ProveedorId { get; set; }
    [DataType(DataType.Date)] public DateTime? FechaEntregaEstimada { get; set; } = DateTime.Today.AddDays(5);
    public string? Observaciones { get; set; }
    public List<LineaVM> Items { get; set; } = new();
}

public class UsuarioVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Escribe el nombre")] public string Nombre { get; set; } = "";
    [Required(ErrorMessage = "Escribe el correo"), EmailAddress] public string Email { get; set; } = "";
    [Required] public string Rol { get; set; } = Roles.Vendedor;
    public bool Activo { get; set; } = true;
    [DataType(DataType.Password), MinLength(6, ErrorMessage = "Mínimo 6 caracteres")] public string? Password { get; set; }
}

public class ReporteVM
{
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }
    public decimal TotalVentas { get; set; }
    public int NumVentas { get; set; }
    public decimal TicketPromedio { get; set; }
    public decimal Utilidad { get; set; }
    public List<string> EtiquetasDias { get; set; } = new();
    public List<decimal> VentasDias { get; set; } = new();
    public List<(string Nombre, int Cantidad, decimal Total)> TopProductos { get; set; } = new();
    public List<(string Vendedor, int Num, decimal Total)> PorVendedor { get; set; } = new();
    public List<(string Metodo, decimal Total)> PorMetodo { get; set; } = new();
    public List<(string Categoria, int Unidades, decimal Valor)> InventarioPorCategoria { get; set; } = new();
    public List<(string Proveedor, int Ordenes, decimal Total)> ComprasPorProveedor { get; set; } = new();
    public decimal AhorroCotizaciones { get; set; }
    public int SolicitudesAdjudicadas { get; set; }
    public List<(string Area, int Num)> EmpleadosPorArea { get; set; } = new();
    public List<(string Eps, int Num)> EmpleadosPorEps { get; set; } = new();
    public decimal Nomina { get; set; }
}
