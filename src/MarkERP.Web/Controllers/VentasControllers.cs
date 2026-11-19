using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

[Authorize(Roles = Roles.AdminVentas)]
public class VentasController : BaseController
{
    private readonly InventarioService _inv;
    private readonly ConfiguracionService _config;
    public VentasController(AppDbContext db, BitacoraService log, InventarioService inv, ConfiguracionService config) : base(db, log)
    { _inv = inv; _config = config; }

    public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string? estado, string? q)
    {
        var d = desde ?? DateTime.Today.AddDays(-30);
        var h = (hasta ?? DateTime.Today).Date.AddDays(1);
        var query = Db.Ventas.Include(v => v.Cliente).Where(v => v.Fecha >= d && v.Fecha < h);
        if (!string.IsNullOrEmpty(estado)) query = query.Where(v => v.Estado == estado);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(v => v.Numero.Contains(q) || (v.Cliente != null && v.Cliente.Nombre.ToLower().Contains(q.ToLower())));
        ViewBag.Desde = d; ViewBag.Hasta = h.AddDays(-1); ViewBag.Estado = estado; ViewBag.Q = q;
        return View(await query.OrderByDescending(v => v.Fecha).ToListAsync());
    }

    public async Task<IActionResult> Nueva()
    {
        await CargarPos();
        return View(new NuevaVentaVM());
    }

    [HttpPost]
    public async Task<IActionResult> Nueva(NuevaVentaVM vm)
    {
        var lineas = vm.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0)
            .GroupBy(i => i.ProductoId).Select(g => new LineaVM { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) }).ToList();
        if (lineas.Count == 0) ModelState.AddModelError("", "Agrega al menos un producto a la venta.");
        if (!ModelState.IsValid) { await CargarPos(); return View(vm); }

        var config = await _config.ObtenerAsync();
        var ids = lineas.Select(l => l.ProductoId).ToList();
        var productos = await Db.Productos.Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        var total = await Db.Ventas.CountAsync();
        var venta = new Venta
        {
            Numero = Consecutivos.Formato("FV", total + 1),
            ClienteId = vm.ClienteId,
            MetodoPago = vm.MetodoPago,
            Vendedor = UsuarioNombre,
            PorcentajeDescuento = vm.PorcentajeDescuento
        };
        try
        {
            foreach (var l in lineas)
            {
                var p = productos[l.ProductoId];
                _inv.Registrar(p, TiposMovimiento.Salida, l.Cantidad, "Venta", venta.Numero, UsuarioNombre);
                venta.Detalles.Add(new DetalleVenta { ProductoId = p.Id, Cantidad = l.Cantidad, PrecioUnitario = p.PrecioVenta, Subtotal = l.Cantidad * p.PrecioVenta });
            }
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await CargarPos();
            return View(vm);
        }
        (venta.Subtotal, venta.Descuento, venta.Iva, venta.Total) =
            Calculos.TotalesVenta(venta.Detalles.Select(d => (d.Cantidad, d.PrecioUnitario)), vm.PorcentajeDescuento, config.PorcentajeIva);
        Db.Ventas.Add(venta);
        Log.Registrar("Ventas", "Venta registrada", $"{venta.Numero} por {Calculos.Moneda(venta.Total)}");
        await Db.SaveChangesAsync();
        Exito($"Venta {venta.Numero} registrada");
        return RedirectToAction(nameof(Detalle), new { id = venta.Id });
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var v = await Cargar(id);
        return v == null ? NotFound() : View(v);
    }

    public async Task<IActionResult> Imprimir(int id)
    {
        var v = await Cargar(id);
        if (v == null) return NotFound();
        ViewBag.Config = await _config.ObtenerAsync();
        return View(v);
    }

    [Authorize(Roles = Roles.Admin), HttpPost]
    public async Task<IActionResult> Anular(int id, string motivo)
    {
        var v = await Cargar(id);
        if (v == null) return NotFound();
        if (v.Estado == EstadosVenta.Anulada) { Error("La venta ya estaba anulada"); return RedirectToAction(nameof(Detalle), new { id }); }
        foreach (var d in v.Detalles)
            _inv.Registrar(d.Producto!, TiposMovimiento.Entrada, d.Cantidad, "Anulación de venta", v.Numero, UsuarioNombre);
        v.Estado = EstadosVenta.Anulada;
        v.MotivoAnulacion = string.IsNullOrWhiteSpace(motivo) ? "Sin motivo" : motivo;
        Log.Registrar("Ventas", "Venta anulada", $"{v.Numero}: {v.MotivoAnulacion}");
        await Db.SaveChangesAsync();
        Exito("Venta anulada y productos devueltos al inventario");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private Task<Venta?> Cargar(int id) =>
        Db.Ventas.Include(v => v.Cliente).Include(v => v.Detalles).ThenInclude(d => d.Producto).FirstOrDefaultAsync(v => v.Id == id);

    private async Task CargarPos()
    {
        ViewBag.Productos = await Db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Clientes = new SelectList(await Db.Clientes.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Metodos = Lista(Listas.MetodosPago);
        ViewBag.Iva = (await _config.ObtenerAsync()).PorcentajeIva;
    }
}

[Authorize(Roles = Roles.AdminVentas)]
public class ClientesController : BaseController
{
    public ClientesController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(string? q)
    {
        var query = Db.Clientes.Include(c => c.Ventas).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(c => c.Nombre.ToLower().Contains(q.ToLower()) || c.Documento.Contains(q));
        ViewBag.Q = q;
        return View(await query.OrderBy(c => c.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var c = await Db.Clientes.Include(x => x.Ventas).FirstOrDefaultAsync(x => x.Id == id);
        return c == null ? NotFound() : View(c);
    }

    public IActionResult Crear()
    {
        ViewBag.TiposDoc = Lista(Listas.TiposDocumento);
        return View("Form", new Cliente());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Cliente c)
    {
        if (await Db.Clientes.AnyAsync(x => x.Documento == c.Documento)) ModelState.AddModelError(nameof(c.Documento), "Ya existe un cliente con ese documento.");
        if (!ModelState.IsValid) { ViewBag.TiposDoc = Lista(Listas.TiposDocumento); return View("Form", c); }
        Db.Clientes.Add(c);
        Log.Registrar("Clientes", "Cliente creado", c.Nombre);
        await Db.SaveChangesAsync();
        Exito("Cliente creado");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(int id)
    {
        var c = await Db.Clientes.FindAsync(id);
        if (c == null) return NotFound();
        ViewBag.TiposDoc = Lista(Listas.TiposDocumento);
        return View("Form", c);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, Cliente form)
    {
        var c = await Db.Clientes.FindAsync(id);
        if (c == null) return NotFound();
        if (!ModelState.IsValid) { ViewBag.TiposDoc = Lista(Listas.TiposDocumento); form.Id = id; return View("Form", form); }
        c.TipoDocumento = form.TipoDocumento; c.Documento = form.Documento; c.Nombre = form.Nombre; c.Telefono = form.Telefono;
        c.Email = form.Email; c.Direccion = form.Direccion; c.Ciudad = form.Ciudad;
        Log.Registrar("Clientes", "Cliente editado", c.Nombre);
        await Db.SaveChangesAsync();
        Exito("Cambios guardados");
        return RedirectToAction(nameof(Detalle), new { id });
    }
}
