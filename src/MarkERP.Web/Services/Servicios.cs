using System.Security.Claims;
using MarkERP.Web.Data;
using MarkERP.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Services;

/// <summary>Registra movimientos de inventario y mantiene el stock actualizado.</summary>
public class InventarioService
{
    private readonly AppDbContext _db;
    public InventarioService(AppDbContext db) => _db = db;

    /// <summary>
    /// Entrada suma, Salida resta y Ajuste fija el stock al valor indicado (conteo físico).
    /// No guarda cambios: el llamador hace SaveChangesAsync para que todo quede en una sola transacción.
    /// </summary>
    public MovimientoInventario Registrar(Producto producto, string tipo, int cantidad, string motivo, string? referencia, string usuario)
    {
        if (cantidad < 0) throw new InvalidOperationException("La cantidad no puede ser negativa.");
        var anterior = producto.StockActual;
        var nuevo = tipo switch
        {
            TiposMovimiento.Entrada => anterior + cantidad,
            TiposMovimiento.Salida => anterior - cantidad,
            TiposMovimiento.Ajuste => cantidad,
            _ => throw new InvalidOperationException("Tipo de movimiento no válido.")
        };
        if (nuevo < 0) throw new InvalidOperationException($"Stock insuficiente para {producto.Nombre}. Disponible: {anterior}.");

        producto.StockActual = nuevo;
        var mov = new MovimientoInventario
        {
            Producto = producto,
            ProductoId = producto.Id,
            Tipo = tipo,
            Cantidad = tipo == TiposMovimiento.Ajuste ? Math.Abs(nuevo - anterior) : cantidad,
            StockAnterior = anterior,
            StockResultante = nuevo,
            Motivo = motivo,
            Referencia = referencia,
            Usuario = usuario,
            Fecha = DateTime.Now
        };
        _db.Movimientos.Add(mov);
        return mov;
    }
}

public class BitacoraService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public BitacoraService(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public void Registrar(string modulo, string accion, string? detalle = null)
    {
        _db.Bitacora.Add(new Bitacora
        {
            Usuario = _http.HttpContext?.User.Identity?.Name ?? "sistema",
            Modulo = modulo,
            Accion = accion,
            Detalle = detalle,
            Fecha = DateTime.Now
        });
    }
}

public class ConfiguracionService
{
    private readonly AppDbContext _db;
    private ConfiguracionNegocio? _cache;
    public ConfiguracionService(AppDbContext db) => _db = db;

    public async Task<ConfiguracionNegocio> ObtenerAsync()
    {
        if (_cache != null) return _cache;
        _cache = await _db.Configuracion.FirstOrDefaultAsync();
        if (_cache == null)
        {
            _cache = new ConfiguracionNegocio();
            _db.Configuracion.Add(_cache);
            await _db.SaveChangesAsync();
        }
        return _cache;
    }
}

public static class UsuarioActual
{
    public static string Nombre(this ClaimsPrincipal user) => user.Identity?.Name ?? "sistema";
    public static string Rol(this ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.Role) ?? "";
    public static bool EsRol(this ClaimsPrincipal user, params string[] roles) => roles.Any(user.IsInRole);
}

public static class Consecutivos
{
    public static string Formato(string prefijo, int numero) => $"{prefijo}-{numero:D5}";
}

/// <summary>Convierte la URL de Render (postgres://usuario:clave@host:puerto/bd) a cadena de conexión de Npgsql.</summary>
public static class ConexionPostgres
{
    public static string DesdeUrl(string url)
    {
        if (!url.StartsWith("postgres", StringComparison.OrdinalIgnoreCase)) return url; // ya es cadena Npgsql
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);
        var usuario = Uri.UnescapeDataString(userInfo[0]);
        var clave = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var puerto = uri.Port > 0 ? uri.Port : 5432;
        var bd = uri.AbsolutePath.Trim('/');
        return $"Host={uri.Host};Port={puerto};Database={bd};Username={usuario};Password={clave};SSL Mode=Prefer";
    }
}
