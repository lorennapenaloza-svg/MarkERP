using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarkERP.Web.Models;

// ───────────── Seguridad y configuración ─────────────

public class Usuario
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
    [Required, EmailAddress, StringLength(120)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [Required] public string Rol { get; set; } = Roles.Vendedor;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? UltimoAcceso { get; set; }
}

public class ConfiguracionNegocio
{
    public int Id { get; set; }
    [Required, StringLength(120), Display(Name = "Nombre del negocio")] public string NombreNegocio { get; set; } = "Mi Tienda";
    [StringLength(30)] public string? Nit { get; set; }
    [StringLength(150), Display(Name = "Dirección")] public string? Direccion { get; set; }
    [StringLength(30), Display(Name = "Teléfono")] public string? Telefono { get; set; }
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    [Range(0, 100), Display(Name = "IVA (%)")] public decimal PorcentajeIva { get; set; } = 19;
    [Display(Name = "Tema por defecto")] public string TemaPorDefecto { get; set; } = "algodon";
    [Display(Name = "Mensaje en facturas")] public string? MensajeFactura { get; set; } = "¡Gracias por tu compra!";
}

public class Bitacora
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public string Usuario { get; set; } = "";
    public string Modulo { get; set; } = "";
    public string Accion { get; set; } = "";
    public string? Detalle { get; set; }
}

// ───────────── Inventario ─────────────

public class Categoria
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [StringLength(200), Display(Name = "Descripción")] public string? Descripcion { get; set; }
    public List<Producto> Productos { get; set; } = new();
}

public class Bodega
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Nombre { get; set; } = "";
    [StringLength(150), Display(Name = "Ubicación")] public string? Ubicacion { get; set; }
    public List<Producto> Productos { get; set; } = new();
}

public class Producto
{
    public int Id { get; set; }
    [Required, StringLength(30), Display(Name = "Código")] public string Codigo { get; set; } = "";
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [StringLength(300), Display(Name = "Descripción")] public string? Descripcion { get; set; }
    [Display(Name = "Categoría")] public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    [Display(Name = "Bodega")] public int BodegaId { get; set; }
    public Bodega? Bodega { get; set; }
    [Range(0, 999999999), Display(Name = "Precio de compra")] public decimal PrecioCompra { get; set; }
    [Range(0, 999999999), Display(Name = "Precio de venta")] public decimal PrecioVenta { get; set; }
    [Range(0, 1000000), Display(Name = "Stock actual")] public int StockActual { get; set; }
    [Range(0, 1000000), Display(Name = "Stock mínimo")] public int StockMinimo { get; set; } = 5;
    public string Unidad { get; set; } = "Unidad";
    public bool Activo { get; set; } = true;

    [NotMapped] public bool StockBajo => StockActual <= StockMinimo;
    [NotMapped] public decimal ValorInventario => StockActual * PrecioCompra;
    [NotMapped] public decimal Margen => PrecioVenta == 0 ? 0 : Math.Round((PrecioVenta - PrecioCompra) / PrecioVenta * 100, 1);
}

public class MovimientoInventario
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public string Tipo { get; set; } = TiposMovimiento.Entrada;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockResultante { get; set; }
    public string Motivo { get; set; } = "";
    public string? Referencia { get; set; }
    public string Usuario { get; set; } = "";
}

// ───────────── Clientes y ventas ─────────────

public class Cliente
{
    public int Id { get; set; }
    [Display(Name = "Tipo de documento")] public string TipoDocumento { get; set; } = "CC";
    [Required, StringLength(20)] public string Documento { get; set; } = "";
    [Required, StringLength(120)] public string Nombre { get; set; } = "";
    [StringLength(30), Display(Name = "Teléfono")] public string? Telefono { get; set; }
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    [StringLength(150), Display(Name = "Dirección")] public string? Direccion { get; set; }
    [StringLength(60)] public string? Ciudad { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
    public List<Venta> Ventas { get; set; } = new();
}

public class Venta
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public string Vendedor { get; set; } = "";
    public string MetodoPago { get; set; } = "Efectivo";
    public decimal Subtotal { get; set; }
    public decimal PorcentajeDescuento { get; set; }
    public decimal Descuento { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = EstadosVenta.Pagada;
    public string? MotivoAnulacion { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = new();
}

public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta? Venta { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

// ───────────── Proveedores, cotizaciones y compras ─────────────

public class Proveedor
{
    public int Id { get; set; }
    [Required, StringLength(20)] public string Nit { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Razón social")] public string RazonSocial { get; set; } = "";
    [StringLength(100), Display(Name = "Persona de contacto")] public string? Contacto { get; set; }
    [StringLength(30), Display(Name = "Teléfono")] public string? Telefono { get; set; }
    [EmailAddress, StringLength(120)] public string? Email { get; set; }
    [StringLength(150), Display(Name = "Dirección")] public string? Direccion { get; set; }
    [StringLength(60)] public string? Ciudad { get; set; }
    [Display(Name = "Condiciones de pago")] public string CondicionesPago { get; set; } = "Contado";
    [Range(0, 365), Display(Name = "Tiempo de entrega (días)")] public int TiempoEntregaDias { get; set; } = 3;
    [Range(1, 5), Display(Name = "Calificación (1 a 5)")] public decimal Calificacion { get; set; } = 4;
    public bool Activo { get; set; } = true;
    [StringLength(300)] public string? Observaciones { get; set; }
    public List<ProveedorProducto> Productos { get; set; } = new();
}

/// <summary>Qué productos vende cada proveedor y a qué precio de referencia.</summary>
public class ProveedorProducto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public decimal PrecioReferencia { get; set; }
    public DateTime FechaActualizacion { get; set; } = DateTime.Now;
}

/// <summary>Solicitud de cotización que se envía a varios proveedores.</summary>
public class SolicitudCotizacion
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public DateTime FechaLimite { get; set; } = DateTime.Today.AddDays(5);
    public string Descripcion { get; set; } = "";
    public string Estado { get; set; } = EstadosSolicitud.Enviada;
    public string CreadaPor { get; set; } = "";
    public int? CotizacionGanadoraId { get; set; }
    public string? MotivoSeleccion { get; set; }
    public decimal AhorroEstimado { get; set; }
    public List<ItemSolicitud> Items { get; set; } = new();
    public List<EnvioCotizacion> Envios { get; set; } = new();
    public List<Cotizacion> Cotizaciones { get; set; } = new();
}

public class ItemSolicitud
{
    public int Id { get; set; }
    public int SolicitudCotizacionId { get; set; }
    public SolicitudCotizacion? SolicitudCotizacion { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
}

public class EnvioCotizacion
{
    public int Id { get; set; }
    public int SolicitudCotizacionId { get; set; }
    public SolicitudCotizacion? SolicitudCotizacion { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateTime FechaEnvio { get; set; } = DateTime.Now;
    public string EmailDestino { get; set; } = "";
    public string Estado { get; set; } = "Enviado (simulado)";
}

/// <summary>Respuesta (cotización) de un proveedor a una solicitud.</summary>
public class Cotizacion
{
    public int Id { get; set; }
    public int SolicitudCotizacionId { get; set; }
    public SolicitudCotizacion? SolicitudCotizacion { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public DateTime FechaRecepcion { get; set; } = DateTime.Now;
    public int TiempoEntregaDias { get; set; }
    public string CondicionesPago { get; set; } = "Contado";
    public int ValidezDias { get; set; } = 15;
    public string? Observaciones { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public decimal Puntaje { get; set; }
    public string Estado { get; set; } = EstadosCotizacion.Recibida;
    public List<DetalleCotizacion> Detalles { get; set; } = new();
}

public class DetalleCotizacion
{
    public int Id { get; set; }
    public int CotizacionId { get; set; }
    public Cotizacion? Cotizacion { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrdenCompra
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public DateTime Fecha { get; set; } = DateTime.Now;
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }
    public int? CotizacionId { get; set; }
    public Cotizacion? Cotizacion { get; set; }
    public string Estado { get; set; } = EstadosOrden.Borrador;
    public DateTime? FechaEntregaEstimada { get; set; }
    public DateTime? FechaRecepcion { get; set; }
    public string CondicionesPago { get; set; } = "Contado";
    public decimal Subtotal { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
    public string? Observaciones { get; set; }
    public string CreadaPor { get; set; } = "";
    public List<DetalleOrdenCompra> Detalles { get; set; } = new();
}

public class DetalleOrdenCompra
{
    public int Id { get; set; }
    public int OrdenCompraId { get; set; }
    public OrdenCompra? OrdenCompra { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

// ───────────── Talento humano ─────────────

public class Empleado
{
    public int Id { get; set; }

    // Datos personales
    [Display(Name = "Tipo de documento")] public string TipoDocumento { get; set; } = "CC";
    [Required, StringLength(20), Display(Name = "Número de documento")] public string NumeroDocumento { get; set; } = "";
    [Required, StringLength(80)] public string Nombres { get; set; } = "";
    [Required, StringLength(80)] public string Apellidos { get; set; } = "";
    [DataType(DataType.Date), Display(Name = "Fecha de nacimiento")] public DateTime FechaNacimiento { get; set; } = DateTime.Today.AddYears(-25);
    [StringLength(80), Display(Name = "Lugar de nacimiento")] public string? LugarNacimiento { get; set; }
    public string Sexo { get; set; } = "Femenino";
    [Display(Name = "Estado civil")] public string EstadoCivil { get; set; } = "Soltero(a)";
    [Range(0, 20), Display(Name = "Personas a cargo")] public int PersonasACargo { get; set; }

    // Datos físicos
    [Range(50, 250), Display(Name = "Estatura (cm)")] public int EstaturaCm { get; set; } = 165;
    [Range(20, 300), Display(Name = "Peso (kg)")] public decimal PesoKg { get; set; } = 60;
    [Display(Name = "Tipo de sangre (RH)")] public string TipoSangre { get; set; } = "O+";
    [Display(Name = "Talla de camisa")] public string? TallaCamisa { get; set; }
    [Display(Name = "Talla de pantalón")] public string? TallaPantalon { get; set; }
    [Display(Name = "Talla de calzado")] public string? TallaCalzado { get; set; }

    // Contacto
    [StringLength(150), Display(Name = "Dirección")] public string? Direccion { get; set; }
    [StringLength(60)] public string? Ciudad { get; set; }
    [StringLength(30), Display(Name = "Teléfono")] public string? Telefono { get; set; }
    [EmailAddress, StringLength(120), Display(Name = "Correo")] public string? Email { get; set; }
    [StringLength(100), Display(Name = "Contacto de emergencia")] public string? ContactoEmergenciaNombre { get; set; }
    [StringLength(40), Display(Name = "Parentesco")] public string? ContactoEmergenciaParentesco { get; set; }
    [StringLength(30), Display(Name = "Teléfono de emergencia")] public string? ContactoEmergenciaTelefono { get; set; }

    // Seguridad social
    [Display(Name = "EPS")] public string Eps { get; set; } = "Sura";
    [Display(Name = "ARL")] public string Arl { get; set; } = "ARL Sura";
    [Display(Name = "Fondo de pensiones")] public string FondoPensiones { get; set; } = "Colpensiones";
    [Display(Name = "Fondo de cesantías")] public string FondoCesantias { get; set; } = "Porvenir";
    [Display(Name = "Caja de compensación")] public string CajaCompensacion { get; set; } = "Compensar";

    // Formación
    [Display(Name = "Nivel educativo")] public string NivelEducativo { get; set; } = "Bachiller";
    [StringLength(100), Display(Name = "Profesión u oficio")] public string? Profesion { get; set; }

    // Datos laborales
    [Required, StringLength(80)] public string Cargo { get; set; } = "";
    [Display(Name = "Área")] public string Area { get; set; } = "Ventas";
    [Display(Name = "Tipo de contrato")] public string TipoContrato { get; set; } = "Término indefinido";
    public string Jornada { get; set; } = "Tiempo completo";
    [DataType(DataType.Date), Display(Name = "Fecha de ingreso")] public DateTime FechaIngreso { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "Fecha de retiro")] public DateTime? FechaRetiro { get; set; }
    [Range(0, 999999999), Display(Name = "Salario mensual")] public decimal Salario { get; set; } = 1423500;
    public string? Banco { get; set; }
    [Display(Name = "Tipo de cuenta")] public string? TipoCuenta { get; set; }
    [StringLength(30), Display(Name = "Número de cuenta")] public string? NumeroCuenta { get; set; }

    [StringLength(500)] public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;

    public List<Turno> Turnos { get; set; } = new();
    public List<Novedad> Novedades { get; set; } = new();

    [NotMapped] public string NombreCompleto => $"{Nombres} {Apellidos}";
    [NotMapped] public string Iniciales => $"{(Nombres.Length > 0 ? Nombres[0] : ' ')}{(Apellidos.Length > 0 ? Apellidos[0] : ' ')}".Trim();
    [NotMapped] public int Edad => Calculos.AniosCumplidos(FechaNacimiento, DateTime.Today);
    [NotMapped] public int AniosAntiguedad => Calculos.AniosCumplidos(FechaIngreso, FechaRetiro ?? DateTime.Today);
    [NotMapped] public string Antiguedad => Calculos.TextoAntiguedad(FechaIngreso, FechaRetiro ?? DateTime.Today);
    [NotMapped] public decimal Imc => EstaturaCm <= 0 ? 0 : Math.Round(PesoKg / (decimal)Math.Pow(EstaturaCm / 100.0, 2), 1);
}

public class Turno
{
    public int Id { get; set; }
    [Display(Name = "Empleado")] public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    [DataType(DataType.Date)] public DateTime Fecha { get; set; } = DateTime.Today;
    [DataType(DataType.Time), Display(Name = "Hora de inicio")] public TimeSpan HoraInicio { get; set; } = new(8, 0, 0);
    [DataType(DataType.Time), Display(Name = "Hora de fin")] public TimeSpan HoraFin { get; set; } = new(16, 0, 0);
    public string Tipo { get; set; } = "Mañana";
    [StringLength(200)] public string? Observacion { get; set; }

    [NotMapped] public double Horas => (HoraFin - HoraInicio).TotalHours;
}

public class Novedad
{
    public int Id { get; set; }
    [Display(Name = "Empleado")] public int EmpleadoId { get; set; }
    public Empleado? Empleado { get; set; }
    public string Tipo { get; set; } = "Permiso";
    [DataType(DataType.Date), Display(Name = "Fecha de inicio")] public DateTime FechaInicio { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "Fecha de fin")] public DateTime FechaFin { get; set; } = DateTime.Today;
    [StringLength(300), Display(Name = "Descripción")] public string? Descripcion { get; set; }
    public string Estado { get; set; } = "Pendiente";

    [NotMapped] public int Dias => (FechaFin.Date - FechaInicio.Date).Days + 1;
}
