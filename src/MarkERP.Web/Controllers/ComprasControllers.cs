using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

[Authorize(Roles = Roles.AdminInventario)]
public class ProveedoresController : BaseController
{
    public ProveedoresController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(string? q, string? ciudad)
    {
        var query = Db.Proveedores.Include(p => p.Productos).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.RazonSocial.ToLower().Contains(q.ToLower()) || p.Nit.Contains(q));
        if (!string.IsNullOrWhiteSpace(ciudad)) query = query.Where(p => p.Ciudad == ciudad);
        ViewBag.Q = q; ViewBag.Ciudad = ciudad;
        ViewBag.Ciudades = await Db.Proveedores.Where(p => p.Ciudad != null).Select(p => p.Ciudad!).Distinct().ToListAsync();
        return View(await query.OrderBy(p => p.RazonSocial).ToListAsync());
    }

    /// <summary>Consulta: ¿quién vende este producto y a qué precio?</summary>
    public async Task<IActionResult> QuienVende(int? productoId)
    {
        ViewBag.Productos = await ProductosSelect(productoId);
        if (productoId == null) return View(new List<ProveedorProducto>());
        ViewBag.Producto = await Db.Productos.FindAsync(productoId);
        var lista = await Db.ProveedorProductos.Include(pp => pp.Proveedor).Where(pp => pp.ProductoId == productoId).ToListAsync();
        return View(lista.OrderBy(pp => pp.PrecioReferencia).ToList());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var p = await Db.Proveedores.Include(x => x.Productos).ThenInclude(pp => pp.Producto).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        ViewBag.Ordenes = await Db.OrdenesCompra.Where(o => o.ProveedorId == id).OrderByDescending(o => o.Fecha).Take(10).ToListAsync();
        ViewBag.Cotizaciones = await Db.Cotizaciones.Include(c => c.SolicitudCotizacion).Where(c => c.ProveedorId == id).OrderByDescending(c => c.FechaRecepcion).Take(10).ToListAsync();
        var ofrecidos = p.Productos.Select(x => x.ProductoId).ToList();
        ViewBag.ProductosDisponibles = new SelectList(await Db.Productos.Where(x => x.Activo && !ofrecidos.Contains(x.Id)).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
        return View(p);
    }

    public IActionResult Crear()
    {
        ViewBag.Condiciones = Lista(Listas.CondicionesPago);
        return View("Form", new Proveedor());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Proveedor p)
    {
        if (await Db.Proveedores.AnyAsync(x => x.Nit == p.Nit)) ModelState.AddModelError(nameof(p.Nit), "Ya existe un proveedor con ese NIT.");
        if (!ModelState.IsValid) { ViewBag.Condiciones = Lista(Listas.CondicionesPago); return View("Form", p); }
        Db.Proveedores.Add(p);
        Log.Registrar("Proveedores", "Proveedor creado", p.RazonSocial);
        await Db.SaveChangesAsync();
        Exito("Proveedor creado. Ahora agrega los productos que vende.");
        return RedirectToAction(nameof(Detalle), new { id = p.Id });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var p = await Db.Proveedores.FindAsync(id);
        if (p == null) return NotFound();
        ViewBag.Condiciones = Lista(Listas.CondicionesPago);
        return View("Form", p);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, Proveedor form)
    {
        var p = await Db.Proveedores.FindAsync(id);
        if (p == null) return NotFound();
        if (!ModelState.IsValid) { ViewBag.Condiciones = Lista(Listas.CondicionesPago); form.Id = id; return View("Form", form); }
        p.Nit = form.Nit; p.RazonSocial = form.RazonSocial; p.Contacto = form.Contacto; p.Telefono = form.Telefono; p.Email = form.Email;
        p.Direccion = form.Direccion; p.Ciudad = form.Ciudad; p.CondicionesPago = form.CondicionesPago;
        p.TiempoEntregaDias = form.TiempoEntregaDias; p.Calificacion = form.Calificacion; p.Activo = form.Activo; p.Observaciones = form.Observaciones;
        Log.Registrar("Proveedores", "Proveedor editado", p.RazonSocial);
        await Db.SaveChangesAsync();
        Exito("Cambios guardados");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AgregarProducto(int id, int productoId, decimal precio)
    {
        if (productoId <= 0 || precio <= 0) { Error("Elige un producto y un precio mayor a cero"); return RedirectToAction(nameof(Detalle), new { id }); }
        if (!await Db.ProveedorProductos.AnyAsync(x => x.ProveedorId == id && x.ProductoId == productoId))
        {
            Db.ProveedorProductos.Add(new ProveedorProducto { ProveedorId = id, ProductoId = productoId, PrecioReferencia = precio });
            Log.Registrar("Proveedores", "Producto asociado a proveedor", $"Proveedor {id}, producto {productoId}");
            await Db.SaveChangesAsync();
            Exito("Producto agregado al proveedor");
        }
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> QuitarProducto(int id, int ppId)
    {
        var pp = await Db.ProveedorProductos.FindAsync(ppId);
        if (pp != null) { Db.ProveedorProductos.Remove(pp); await Db.SaveChangesAsync(); Exito("Producto quitado"); }
        return RedirectToAction(nameof(Detalle), new { id });
    }
}

[Authorize(Roles = Roles.AdminInventario)]
public class CotizacionesController : BaseController
{
    private readonly ConfiguracionService _config;
    public CotizacionesController(AppDbContext db, BitacoraService log, ConfiguracionService config) : base(db, log) => _config = config;

    public async Task<IActionResult> Index(string? estado)
    {
        var q = Db.Solicitudes.Include(s => s.Items).Include(s => s.Envios).Include(s => s.Cotizaciones).AsQueryable();
        if (!string.IsNullOrEmpty(estado)) q = q.Where(s => s.Estado == estado);
        ViewBag.Estado = estado;
        return View(await q.OrderByDescending(s => s.Fecha).ToListAsync());
    }

    public async Task<IActionResult> Nueva(int? productoId)
    {
        await CargarListas();
        var vm = new NuevaSolicitudVM();
        if (productoId.HasValue)
        {
            var p = await Db.Productos.FindAsync(productoId.Value);
            if (p != null)
            {
                vm.Items.Add(new LineaVM { ProductoId = p.Id, Cantidad = Math.Max(1, p.StockMinimo * 2 - p.StockActual) });
                vm.Descripcion = $"Reposición de {p.Nombre}";
                vm.ProveedoresIds = await Db.ProveedorProductos.Where(x => x.ProductoId == p.Id).Select(x => x.ProveedorId).ToListAsync();
            }
        }
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Nueva(NuevaSolicitudVM vm)
    {
        var items = vm.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0).GroupBy(i => i.ProductoId)
            .Select(g => new LineaVM { ProductoId = g.Key, Cantidad = g.Sum(x => x.Cantidad) }).ToList();
        if (items.Count == 0) ModelState.AddModelError("", "Agrega al menos un producto.");
        if (vm.ProveedoresIds.Count == 0) ModelState.AddModelError("", "Elige al menos un proveedor para enviar la solicitud.");
        if (!ModelState.IsValid) { await CargarListas(); return View(vm); }

        var numero = await Db.Solicitudes.CountAsync() + 1;
        var sol = new SolicitudCotizacion
        {
            Numero = Consecutivos.Formato("SC", numero), Descripcion = vm.Descripcion, FechaLimite = vm.FechaLimite,
            CreadaPor = UsuarioNombre, Estado = EstadosSolicitud.Enviada
        };
        foreach (var i in items) sol.Items.Add(new ItemSolicitud { ProductoId = i.ProductoId, Cantidad = i.Cantidad });
        var proveedores = await Db.Proveedores.Where(p => vm.ProveedoresIds.Contains(p.Id)).ToListAsync();
        foreach (var p in proveedores)
            sol.Envios.Add(new EnvioCotizacion { ProveedorId = p.Id, EmailDestino = p.Email ?? "(sin correo)" });
        Db.Solicitudes.Add(sol);
        Log.Registrar("Cotizaciones", "Solicitud enviada", $"{sol.Numero} a {proveedores.Count} proveedores");
        await Db.SaveChangesAsync();
        Exito($"Solicitud {sol.Numero} enviada a {proveedores.Count} proveedores (envío simulado)");
        return RedirectToAction(nameof(Detalle), new { id = sol.Id });
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        ViewBag.Recomendada = EvaluadorCotizaciones.Recomendada(s.Cotizaciones);
        ViewBag.Orden = await Db.OrdenesCompra.FirstOrDefaultAsync(o => o.CotizacionId == s.CotizacionGanadoraId && s.CotizacionGanadoraId != null);
        return View(s);
    }

    /// <summary>Vista imprimible de la solicitud para un proveedor (se guarda como PDF desde el navegador).</summary>
    public async Task<IActionResult> Imprimir(int id, int proveedorId)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        ViewBag.Proveedor = await Db.Proveedores.FindAsync(proveedorId);
        ViewBag.Config = await _config.ObtenerAsync();
        return View(s);
    }

    public async Task<IActionResult> Responder(int id, int proveedorId)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        var prov = await Db.Proveedores.FindAsync(proveedorId);
        if (prov == null) return NotFound();
        var precios = await Db.ProveedorProductos.Where(x => x.ProveedorId == proveedorId).ToDictionaryAsync(x => x.ProductoId, x => x.PrecioReferencia);
        ViewBag.Solicitud = s; ViewBag.Proveedor = prov; ViewBag.Condiciones = Lista(Listas.CondicionesPago, prov.CondicionesPago);
        return View(new RespuestaCotizacionVM
        {
            SolicitudId = id, ProveedorId = proveedorId, TiempoEntregaDias = prov.TiempoEntregaDias, CondicionesPago = prov.CondicionesPago,
            Items = s.Items.Select(i => new LineaVM
            {
                ProductoId = i.ProductoId, Cantidad = i.Cantidad,
                PrecioUnitario = precios.TryGetValue(i.ProductoId, out var pr) ? pr : i.Producto!.PrecioCompra
            }).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> Responder(RespuestaCotizacionVM vm)
    {
        var s = await Cargar(vm.SolicitudId);
        if (s == null) return NotFound();
        if (vm.Items.Any(i => i.PrecioUnitario <= 0)) ModelState.AddModelError("", "Todos los precios deben ser mayores a cero.");
        if (!ModelState.IsValid)
        {
            ViewBag.Solicitud = s; ViewBag.Proveedor = await Db.Proveedores.FindAsync(vm.ProveedorId);
            ViewBag.Condiciones = Lista(Listas.CondicionesPago, vm.CondicionesPago);
            return View(vm);
        }
        var config = await _config.ObtenerAsync();
        var previa = s.Cotizaciones.FirstOrDefault(c => c.ProveedorId == vm.ProveedorId);
        if (previa != null) { Db.DetallesCotizacion.RemoveRange(previa.Detalles); s.Cotizaciones.Remove(previa); Db.Cotizaciones.Remove(previa); }

        var cot = CrearCotizacion(s, vm.ProveedorId, vm.TiempoEntregaDias, vm.CondicionesPago, vm.ValidezDias, vm.Observaciones,
            vm.Items.ToDictionary(i => i.ProductoId, i => i.PrecioUnitario), config.PorcentajeIva);
        cot.Proveedor = await Db.Proveedores.FindAsync(vm.ProveedorId);
        s.Cotizaciones.Add(cot);
        if (s.Estado == EstadosSolicitud.Enviada) s.Estado = EstadosSolicitud.EnEvaluacion;
        EvaluadorCotizaciones.Puntuar(s.Cotizaciones);
        Log.Registrar("Cotizaciones", "Cotización recibida", $"{s.Numero} - {cot.Proveedor?.RazonSocial}: {Calculos.Moneda(cot.Total)}");
        await Db.SaveChangesAsync();
        Exito("Cotización registrada y comparativo actualizado");
        return RedirectToAction(nameof(Detalle), new { id = s.Id });
    }

    /// <summary>Para la demo: cada proveedor que aún no respondió envía una cotización con precios cercanos a su referencia.</summary>
    [HttpPost]
    public async Task<IActionResult> SimularRespuestas(int id)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        var config = await _config.ObtenerAsync();
        var rnd = new Random();
        var nuevas = 0;
        foreach (var envio in s.Envios.Where(e => s.Cotizaciones.All(c => c.ProveedorId != e.ProveedorId)))
        {
            var refs = await Db.ProveedorProductos.Where(x => x.ProveedorId == envio.ProveedorId).ToDictionaryAsync(x => x.ProductoId, x => x.PrecioReferencia);
            var precios = s.Items.ToDictionary(i => i.ProductoId, i =>
            {
                var basePrecio = refs.TryGetValue(i.ProductoId, out var r) ? r : i.Producto!.PrecioCompra;
                return Calculos.Redondear(basePrecio * (0.92m + (decimal)rnd.NextDouble() * 0.18m) / 100) * 100;
            });
            var prov = envio.Proveedor!;
            var cot = CrearCotizacion(s, prov.Id, Math.Max(1, prov.TiempoEntregaDias + rnd.Next(-1, 3)), prov.CondicionesPago, 15,
                "Respuesta simulada para la demostración.", precios, config.PorcentajeIva);
            cot.Proveedor = prov;
            s.Cotizaciones.Add(cot);
            nuevas++;
        }
        if (nuevas > 0 && s.Estado == EstadosSolicitud.Enviada) s.Estado = EstadosSolicitud.EnEvaluacion;
        EvaluadorCotizaciones.Puntuar(s.Cotizaciones);
        Log.Registrar("Cotizaciones", "Respuestas simuladas", $"{s.Numero}: {nuevas}");
        await Db.SaveChangesAsync();
        if (nuevas > 0) Exito($"Se recibieron {nuevas} cotizaciones simuladas"); else Error("Todos los proveedores ya respondieron");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Seleccionar(int id, int cotizacionId, string? motivo)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        var elegida = s.Cotizaciones.FirstOrDefault(c => c.Id == cotizacionId);
        if (elegida == null) return NotFound();
        foreach (var c in s.Cotizaciones) c.Estado = c.Id == cotizacionId ? EstadosCotizacion.Seleccionada : EstadosCotizacion.Descartada;
        s.CotizacionGanadoraId = cotizacionId;
        s.Estado = EstadosSolicitud.Adjudicada;
        var recomendada = EvaluadorCotizaciones.Recomendada(s.Cotizaciones);
        s.MotivoSeleccion = string.IsNullOrWhiteSpace(motivo)
            ? (recomendada?.Id == cotizacionId ? "Cotización con mejor puntaje (precio, entrega y calificación)." : "Selección manual")
            : motivo;
        s.AhorroEstimado = s.Cotizaciones.Max(c => c.Total) - elegida.Total;
        Log.Registrar("Cotizaciones", "Cotización seleccionada", $"{s.Numero}: {elegida.Proveedor?.RazonSocial}");
        await Db.SaveChangesAsync();
        Exito($"Te quedaste con la cotización de {elegida.Proveedor?.RazonSocial}. Ya puedes generar la orden de compra.");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> GenerarOrden(int id)
    {
        var s = await Cargar(id);
        if (s == null) return NotFound();
        var cot = s.Cotizaciones.FirstOrDefault(c => c.Id == s.CotizacionGanadoraId);
        if (cot == null) { Error("Primero selecciona una cotización"); return RedirectToAction(nameof(Detalle), new { id }); }
        var existente = await Db.OrdenesCompra.FirstOrDefaultAsync(o => o.CotizacionId == cot.Id);
        if (existente != null) return RedirectToAction("Detalle", "OrdenesCompra", new { id = existente.Id });

        var numero = await Db.OrdenesCompra.CountAsync() + 1;
        var oc = new OrdenCompra
        {
            Numero = Consecutivos.Formato("OC", numero), ProveedorId = cot.ProveedorId, CotizacionId = cot.Id, Estado = EstadosOrden.Borrador,
            CondicionesPago = cot.CondicionesPago, FechaEntregaEstimada = DateTime.Today.AddDays(cot.TiempoEntregaDias),
            Subtotal = cot.Subtotal, Iva = cot.Iva, Total = cot.Total, CreadaPor = UsuarioNombre, Observaciones = $"Generada desde la solicitud {s.Numero}"
        };
        foreach (var d in cot.Detalles)
            oc.Detalles.Add(new DetalleOrdenCompra { ProductoId = d.ProductoId, Cantidad = d.Cantidad, PrecioUnitario = d.PrecioUnitario, Subtotal = d.Subtotal });
        Db.OrdenesCompra.Add(oc);
        Log.Registrar("Compras", "Orden de compra generada", $"{oc.Numero} desde {s.Numero}");
        await Db.SaveChangesAsync();
        Exito($"Orden de compra {oc.Numero} generada");
        return RedirectToAction("Detalle", "OrdenesCompra", new { id = oc.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id)
    {
        var s = await Db.Solicitudes.FindAsync(id);
        if (s == null) return NotFound();
        s.Estado = EstadosSolicitud.Cancelada;
        Log.Registrar("Cotizaciones", "Solicitud cancelada", s.Numero);
        await Db.SaveChangesAsync();
        Exito("Solicitud cancelada");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private static Cotizacion CrearCotizacion(SolicitudCotizacion s, int proveedorId, int dias, string condiciones, int validez,
        string? obs, Dictionary<int, decimal> precios, decimal iva)
    {
        var cot = new Cotizacion
        {
            SolicitudCotizacionId = s.Id, ProveedorId = proveedorId, TiempoEntregaDias = dias, CondicionesPago = condiciones,
            ValidezDias = validez, Observaciones = obs
        };
        foreach (var it in s.Items)
        {
            var precio = precios.TryGetValue(it.ProductoId, out var p) ? p : 0;
            cot.Detalles.Add(new DetalleCotizacion { ProductoId = it.ProductoId, Cantidad = it.Cantidad, PrecioUnitario = precio, Subtotal = precio * it.Cantidad });
        }
        cot.Subtotal = cot.Detalles.Sum(d => d.Subtotal);
        cot.Iva = Calculos.Redondear(cot.Subtotal * iva / 100m);
        cot.Total = cot.Subtotal + cot.Iva;
        return cot;
    }

    private Task<SolicitudCotizacion?> Cargar(int id) => Db.Solicitudes
        .Include(s => s.Items).ThenInclude(i => i.Producto)
        .Include(s => s.Envios).ThenInclude(e => e.Proveedor)
        .Include(s => s.Cotizaciones).ThenInclude(c => c.Proveedor)
        .Include(s => s.Cotizaciones).ThenInclude(c => c.Detalles)
        .AsSplitQuery()
        .FirstOrDefaultAsync(s => s.Id == id);

    private async Task CargarListas()
    {
        ViewBag.Productos = await Db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
        ViewBag.Proveedores = await Db.Proveedores.Where(p => p.Activo).Include(p => p.Productos).OrderBy(p => p.RazonSocial).ToListAsync();
    }
}

[Authorize(Roles = Roles.AdminInventario)]
public class OrdenesCompraController : BaseController
{
    private readonly InventarioService _inv;
    private readonly ConfiguracionService _config;
    public OrdenesCompraController(AppDbContext db, BitacoraService log, InventarioService inv, ConfiguracionService config) : base(db, log)
    { _inv = inv; _config = config; }

    public async Task<IActionResult> Index(string? estado)
    {
        var q = Db.OrdenesCompra.Include(o => o.Proveedor).AsQueryable();
        if (!string.IsNullOrEmpty(estado)) q = q.Where(o => o.Estado == estado);
        ViewBag.Estado = estado;
        return View(await q.OrderByDescending(o => o.Fecha).ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var o = await Cargar(id);
        return o == null ? NotFound() : View(o);
    }

    public async Task<IActionResult> Imprimir(int id)
    {
        var o = await Cargar(id);
        if (o == null) return NotFound();
        ViewBag.Config = await _config.ObtenerAsync();
        return View(o);
    }

    public async Task<IActionResult> Nueva()
    {
        await CargarListas();
        return View(new NuevaOrdenVM());
    }

    [HttpPost]
    public async Task<IActionResult> Nueva(NuevaOrdenVM vm)
    {
        var items = vm.Items.Where(i => i.ProductoId > 0 && i.Cantidad > 0 && i.PrecioUnitario > 0).ToList();
        if (vm.ProveedorId <= 0) ModelState.AddModelError("", "Elige un proveedor.");
        if (items.Count == 0) ModelState.AddModelError("", "Agrega al menos un producto con cantidad y precio.");
        if (!ModelState.IsValid) { await CargarListas(); return View(vm); }
        var config = await _config.ObtenerAsync();
        var prov = await Db.Proveedores.FindAsync(vm.ProveedorId);
        var oc = new OrdenCompra
        {
            Numero = Consecutivos.Formato("OC", await Db.OrdenesCompra.CountAsync() + 1), ProveedorId = vm.ProveedorId,
            FechaEntregaEstimada = vm.FechaEntregaEstimada, Observaciones = vm.Observaciones, CreadaPor = UsuarioNombre,
            CondicionesPago = prov?.CondicionesPago ?? "Contado"
        };
        foreach (var i in items)
            oc.Detalles.Add(new DetalleOrdenCompra { ProductoId = i.ProductoId, Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario, Subtotal = i.Cantidad * i.PrecioUnitario });
        oc.Subtotal = oc.Detalles.Sum(d => d.Subtotal);
        oc.Iva = Calculos.Redondear(oc.Subtotal * config.PorcentajeIva / 100m);
        oc.Total = oc.Subtotal + oc.Iva;
        Db.OrdenesCompra.Add(oc);
        Log.Registrar("Compras", "Orden de compra creada", oc.Numero);
        await Db.SaveChangesAsync();
        Exito($"Orden {oc.Numero} creada");
        return RedirectToAction(nameof(Detalle), new { id = oc.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Enviar(int id)
    {
        var o = await Cargar(id);
        if (o == null) return NotFound();
        if (o.Estado != EstadosOrden.Borrador) { Error("Solo se pueden enviar órdenes en borrador"); return RedirectToAction(nameof(Detalle), new { id }); }
        o.Estado = EstadosOrden.Enviada;
        Log.Registrar("Compras", "Orden enviada al proveedor (simulado)", $"{o.Numero} a {o.Proveedor?.Email}");
        await Db.SaveChangesAsync();
        Exito($"Orden enviada a {o.Proveedor?.Email} (envío simulado)");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    /// <summary>Recibir la mercancía: suma al inventario y actualiza precios de compra.</summary>
    [HttpPost]
    public async Task<IActionResult> Recibir(int id)
    {
        var o = await Cargar(id);
        if (o == null) return NotFound();
        if (o.Estado is EstadosOrden.Recibida or EstadosOrden.Cancelada) { Error("Esta orden ya no se puede recibir"); return RedirectToAction(nameof(Detalle), new { id }); }
        foreach (var d in o.Detalles)
        {
            _inv.Registrar(d.Producto!, TiposMovimiento.Entrada, d.Cantidad, "Recepción de orden de compra", o.Numero, UsuarioNombre);
            d.Producto!.PrecioCompra = d.PrecioUnitario;
            var pp = await Db.ProveedorProductos.FirstOrDefaultAsync(x => x.ProveedorId == o.ProveedorId && x.ProductoId == d.ProductoId);
            if (pp == null) Db.ProveedorProductos.Add(new ProveedorProducto { ProveedorId = o.ProveedorId, ProductoId = d.ProductoId, PrecioReferencia = d.PrecioUnitario });
            else { pp.PrecioReferencia = d.PrecioUnitario; pp.FechaActualizacion = DateTime.Now; }
        }
        o.Estado = EstadosOrden.Recibida;
        o.FechaRecepcion = DateTime.Now;
        Log.Registrar("Compras", "Orden recibida", o.Numero);
        await Db.SaveChangesAsync();
        Exito("Mercancía recibida: el inventario se actualizó");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancelar(int id)
    {
        var o = await Db.OrdenesCompra.FindAsync(id);
        if (o == null) return NotFound();
        if (o.Estado == EstadosOrden.Recibida) { Error("No se puede cancelar una orden recibida"); return RedirectToAction(nameof(Detalle), new { id }); }
        o.Estado = EstadosOrden.Cancelada;
        Log.Registrar("Compras", "Orden cancelada", o.Numero);
        await Db.SaveChangesAsync();
        Exito("Orden cancelada");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private Task<OrdenCompra?> Cargar(int id) => Db.OrdenesCompra
        .Include(o => o.Proveedor).Include(o => o.Cotizacion).ThenInclude(c => c!.SolicitudCotizacion)
        .Include(o => o.Detalles).ThenInclude(d => d.Producto)
        .FirstOrDefaultAsync(o => o.Id == id);

    private async Task CargarListas()
    {
        ViewBag.Proveedores = new SelectList(await Db.Proveedores.Where(p => p.Activo).OrderBy(p => p.RazonSocial).ToListAsync(), "Id", "RazonSocial");
        ViewBag.Productos = await Db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
    }
}
