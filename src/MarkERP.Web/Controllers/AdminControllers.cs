using System.Text;
using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class ReportesController : BaseController
{
    public ReportesController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-29)).Date;
        var h = (hasta ?? DateTime.Today).Date;
        var fin = h.AddDays(1);

        var ventas = await Db.Ventas.Include(v => v.Detalles).ThenInclude(x => x.Producto).ThenInclude(p => p!.Categoria)
            .Where(v => v.Fecha >= d && v.Fecha < fin && v.Estado == EstadosVenta.Pagada).ToListAsync();
        var detalles = ventas.SelectMany(v => v.Detalles).ToList();
        var productos = await Db.Productos.Include(p => p.Categoria).Where(p => p.Activo).ToListAsync();
        var ordenes = await Db.OrdenesCompra.Include(o => o.Proveedor).Where(o => o.Fecha >= d && o.Fecha < fin && o.Estado != EstadosOrden.Cancelada).ToListAsync();
        var adjudicadas = await Db.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Adjudicada && s.Fecha >= d && s.Fecha < fin).ToListAsync();
        var empleados = await Db.Empleados.Where(e => e.Activo).ToListAsync();

        var vm = new ReporteVM
        {
            Desde = d, Hasta = h,
            TotalVentas = ventas.Sum(v => v.Total),
            NumVentas = ventas.Count,
            TicketPromedio = ventas.Count == 0 ? 0 : Calculos.Redondear(ventas.Average(v => v.Total)),
            Utilidad = detalles.Sum(x => (x.PrecioUnitario - (x.Producto?.PrecioCompra ?? 0)) * x.Cantidad),
            TopProductos = detalles.GroupBy(x => x.Producto!.Nombre).Select(g => (g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Subtotal)))
                .OrderByDescending(x => x.Item2).Take(10).ToList(),
            PorVendedor = ventas.GroupBy(v => v.Vendedor).Select(g => (g.Key, g.Count(), g.Sum(v => v.Total))).OrderByDescending(x => x.Item3).ToList(),
            PorMetodo = ventas.GroupBy(v => v.MetodoPago).Select(g => (g.Key, g.Sum(v => v.Total))).OrderByDescending(x => x.Item2).ToList(),
            InventarioPorCategoria = productos.GroupBy(p => p.Categoria?.Nombre ?? "Sin categoría")
                .Select(g => (g.Key, g.Sum(p => p.StockActual), g.Sum(p => p.ValorInventario))).OrderByDescending(x => x.Item3).ToList(),
            ComprasPorProveedor = ordenes.GroupBy(o => o.Proveedor?.RazonSocial ?? "-").Select(g => (g.Key, g.Count(), g.Sum(o => o.Total)))
                .OrderByDescending(x => x.Item3).ToList(),
            AhorroCotizaciones = adjudicadas.Sum(s => s.AhorroEstimado),
            SolicitudesAdjudicadas = adjudicadas.Count,
            EmpleadosPorArea = empleados.GroupBy(e => e.Area).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList(),
            EmpleadosPorEps = empleados.GroupBy(e => e.Eps).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).ToList(),
            Nomina = empleados.Sum(e => e.Salario)
        };
        for (var dia = d; dia <= h; dia = dia.AddDays(1))
        {
            vm.EtiquetasDias.Add(dia.ToString("dd MMM", Calculos.Co));
            vm.VentasDias.Add(ventas.Where(v => v.Fecha.Date == dia).Sum(v => v.Total));
        }
        return View(vm);
    }

    public async Task<IActionResult> ExportarVentas(DateTime? desde, DateTime? hasta)
    {
        var d = (desde ?? DateTime.Today.AddDays(-29)).Date;
        var fin = (hasta ?? DateTime.Today).Date.AddDays(1);
        var ventas = await Db.Ventas.Include(v => v.Cliente).Where(v => v.Fecha >= d && v.Fecha < fin).OrderBy(v => v.Fecha).ToListAsync();
        var sb = new StringBuilder("Número;Fecha;Cliente;Vendedor;Método de pago;Subtotal;Descuento;IVA;Total;Estado\n");
        foreach (var v in ventas)
            sb.AppendLine(string.Join(";", v.Numero, v.Fecha.ToString("yyyy-MM-dd HH:mm"), v.Cliente?.Nombre ?? "Consumidor final", v.Vendedor,
                v.MetodoPago, v.Subtotal, v.Descuento, v.Iva, v.Total, v.Estado));
        return Csv(sb, "ventas_markerp.csv");
    }

    public async Task<IActionResult> ExportarInventario()
    {
        var productos = await Db.Productos.Include(p => p.Categoria).Include(p => p.Bodega).OrderBy(p => p.Nombre).ToListAsync();
        var sb = new StringBuilder("Código;Producto;Categoría;Bodega;Stock;Stock mínimo;Precio compra;Precio venta;Valor inventario;Estado\n");
        foreach (var p in productos)
            sb.AppendLine(string.Join(";", p.Codigo, p.Nombre, p.Categoria?.Nombre, p.Bodega?.Nombre, p.StockActual, p.StockMinimo,
                p.PrecioCompra, p.PrecioVenta, p.ValorInventario, p.StockBajo ? "Stock bajo" : "OK"));
        return Csv(sb, "inventario_markerp.csv");
    }

    private FileContentResult Csv(StringBuilder sb, string nombre) =>
        File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv", nombre);
}

[Authorize(Roles = Roles.Admin)]
public class UsuariosController : BaseController
{
    public UsuariosController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index() => View(await Db.Usuarios.OrderBy(u => u.Nombre).ToListAsync());

    public IActionResult Crear() { ViewBag.Roles = Lista(Roles.Todos); return View("Form", new UsuarioVM()); }

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Password)) ModelState.AddModelError(nameof(vm.Password), "Escribe una contraseña.");
        if (await Db.Usuarios.AnyAsync(u => u.Email.ToLower() == vm.Email.ToLower())) ModelState.AddModelError(nameof(vm.Email), "Ese correo ya está registrado.");
        if (!ModelState.IsValid) { ViewBag.Roles = Lista(Roles.Todos); return View("Form", vm); }
        var u = new Usuario { Nombre = vm.Nombre, Email = vm.Email.Trim().ToLower(), Rol = vm.Rol, Activo = vm.Activo };
        u.PasswordHash = new PasswordHasher<Usuario>().HashPassword(u, vm.Password!);
        Db.Usuarios.Add(u);
        Log.Registrar("Usuarios", "Usuario creado", $"{u.Email} ({u.Rol})");
        await Db.SaveChangesAsync();
        Exito("Usuario creado");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var u = await Db.Usuarios.FindAsync(id);
        if (u == null) return NotFound();
        ViewBag.Roles = Lista(Roles.Todos);
        return View("Form", new UsuarioVM { Id = u.Id, Nombre = u.Nombre, Email = u.Email, Rol = u.Rol, Activo = u.Activo });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, UsuarioVM vm)
    {
        var u = await Db.Usuarios.FindAsync(id);
        if (u == null) return NotFound();
        if (await Db.Usuarios.AnyAsync(x => x.Email.ToLower() == vm.Email.ToLower() && x.Id != id)) ModelState.AddModelError(nameof(vm.Email), "Ese correo ya está registrado.");
        if (!ModelState.IsValid) { ViewBag.Roles = Lista(Roles.Todos); vm.Id = id; return View("Form", vm); }
        u.Nombre = vm.Nombre; u.Email = vm.Email.Trim().ToLower(); u.Rol = vm.Rol; u.Activo = vm.Activo;
        if (!string.IsNullOrWhiteSpace(vm.Password)) u.PasswordHash = new PasswordHasher<Usuario>().HashPassword(u, vm.Password);
        Log.Registrar("Usuarios", "Usuario editado", u.Email);
        await Db.SaveChangesAsync();
        Exito("Usuario actualizado");
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = Roles.Admin)]
public class ConfiguracionController : BaseController
{
    private readonly ConfiguracionService _config;
    public ConfiguracionController(AppDbContext db, BitacoraService log, ConfiguracionService config) : base(db, log) => _config = config;

    public async Task<IActionResult> Index() => View(await _config.ObtenerAsync());

    [HttpPost]
    public async Task<IActionResult> Index(ConfiguracionNegocio form)
    {
        var c = await _config.ObtenerAsync();
        if (!ModelState.IsValid) return View(form);
        c.NombreNegocio = form.NombreNegocio; c.Nit = form.Nit; c.Direccion = form.Direccion; c.Telefono = form.Telefono;
        c.Email = form.Email; c.PorcentajeIva = form.PorcentajeIva; c.TemaPorDefecto = form.TemaPorDefecto; c.MensajeFactura = form.MensajeFactura;
        Log.Registrar("Configuración", "Datos del negocio actualizados");
        await Db.SaveChangesAsync();
        Exito("Configuración guardada");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Bitacora(string? modulo)
    {
        var q = Db.Bitacora.AsQueryable();
        if (!string.IsNullOrEmpty(modulo)) q = q.Where(b => b.Modulo == modulo);
        ViewBag.Modulo = modulo;
        ViewBag.Modulos = await Db.Bitacora.Select(b => b.Modulo).Distinct().OrderBy(m => m).ToListAsync();
        return View(await q.OrderByDescending(b => b.Fecha).Take(300).ToListAsync());
    }
}
