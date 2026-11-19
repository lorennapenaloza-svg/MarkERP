namespace MarkERP.Web.Models;

public static class Roles
{
    public const string Admin = "Administrador";
    public const string Vendedor = "Vendedor";
    public const string Inventario = "Inventario";
    public const string TalentoHumano = "TalentoHumano";

    public const string AdminVentas = Admin + "," + Vendedor;
    public const string AdminInventario = Admin + "," + Inventario;
    public const string AdminTH = Admin + "," + TalentoHumano;

    public static readonly string[] Todos = { Admin, Vendedor, Inventario, TalentoHumano };

    public static string Nombre(string rol) => rol switch
    {
        Admin => "Administrador",
        Vendedor => "Vendedor",
        Inventario => "Encargado de inventario",
        TalentoHumano => "Talento humano",
        _ => rol
    };
}

public static class TiposMovimiento
{
    public const string Entrada = "Entrada";
    public const string Salida = "Salida";
    public const string Ajuste = "Ajuste";
}

public static class EstadosVenta
{
    public const string Pagada = "Pagada";
    public const string Anulada = "Anulada";
}

public static class EstadosSolicitud
{
    public const string Enviada = "Enviada";
    public const string EnEvaluacion = "En evaluación";
    public const string Adjudicada = "Adjudicada";
    public const string Cancelada = "Cancelada";
}

public static class EstadosCotizacion
{
    public const string Recibida = "Recibida";
    public const string Seleccionada = "Seleccionada";
    public const string Descartada = "Descartada";
}

public static class EstadosOrden
{
    public const string Borrador = "Borrador";
    public const string Enviada = "Enviada";
    public const string Recibida = "Recibida";
    public const string Cancelada = "Cancelada";
}

public static class Listas
{
    public static readonly string[] TiposDocumento = { "CC", "CE", "TI", "PEP", "Pasaporte", "NIT" };
    public static readonly string[] MetodosPago = { "Efectivo", "Tarjeta débito", "Tarjeta crédito", "Nequi", "Daviplata", "Transferencia" };
    public static readonly string[] Eps = { "Sura", "Sanitas", "Nueva EPS", "Compensar", "Salud Total", "Famisanar", "Coosalud", "Aliansalud", "Mutual Ser" };
    public static readonly string[] Arl = { "ARL Sura", "Positiva", "Colmena", "Bolívar", "AXA Colpatria", "Equidad" };
    public static readonly string[] FondosPension = { "Colpensiones", "Porvenir", "Protección", "Colfondos", "Skandia" };
    public static readonly string[] FondosCesantias = { "Porvenir", "Protección", "Colfondos", "Skandia", "Fondo Nacional del Ahorro" };
    public static readonly string[] Cajas = { "Compensar", "Cafam", "Colsubsidio", "Comfenalco", "Comfama", "Comfacundi" };
    public static readonly string[] TiposSangre = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
    public static readonly string[] EstadosCiviles = { "Soltero(a)", "Casado(a)", "Unión libre", "Separado(a)", "Viudo(a)" };
    public static readonly string[] Sexos = { "Femenino", "Masculino", "Otro" };
    public static readonly string[] TiposContrato = { "Término indefinido", "Término fijo", "Obra o labor", "Prestación de servicios", "Aprendizaje" };
    public static readonly string[] Jornadas = { "Tiempo completo", "Medio tiempo", "Por horas" };
    public static readonly string[] Areas = { "Ventas", "Inventario y bodega", "Administración", "Talento humano", "Compras" };
    public static readonly string[] NivelesEducativos = { "Bachiller", "Técnico", "Tecnólogo", "Profesional", "Especialización", "Maestría" };
    public static readonly string[] TiposCuenta = { "Ahorros", "Corriente" };
    public static readonly string[] Bancos = { "Bancolombia", "Davivienda", "Banco de Bogotá", "BBVA", "Banco Caja Social", "Nequi" };
    public static readonly string[] TiposTurno = { "Mañana", "Tarde", "Noche", "Completo" };
    public static readonly string[] TiposNovedad = { "Incapacidad", "Vacaciones", "Permiso", "Licencia", "Calamidad doméstica" };
    public static readonly string[] EstadosNovedad = { "Pendiente", "Aprobada", "Rechazada" };
    public static readonly string[] CondicionesPago = { "Contado", "Crédito 15 días", "Crédito 30 días", "Crédito 60 días" };
    public static readonly string[] Unidades = { "Unidad", "Paquete", "Caja", "Docena", "Par" };
}
