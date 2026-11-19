using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

public class ProductosController : BaseController
{
    private readonly InventarioService _inv;
    public ProductosController(AppDbContext db, BitacoraService log, InventarioService inv) : base(db, log) => _inv = inv;

    public async Task<IActionResult> Index(string? q, int? categoriaId, bool soloBajo = false)
    {
        var query = Db.Productos.Include(p => p.Categoria).Include(p => p.Bodega).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Nombre.ToLower().Contains(q.ToLower()) || p.Codigo.ToLower().Contains(q.ToLower()));
        if (categoriaId.HasValue) query = query.Where(p => p.CategoriaId == categoriaId);
        if (soloBajo) query = query.Where(p => p.StockActual <= p.StockMinimo);
        ViewBag.Categorias = new SelectList(await Db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", categoriaId);
        ViewBag.Q = q; ViewBag.SoloBajo = soloBajo;
        return View(await query.OrderBy(p => p.Nombre).ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var p = await Db.Productos.Include(x => x.Categoria).Include(x => x.Bodega).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        ViewBag.Movimientos = await Db.Movimientos.Where(m => m.ProductoId == id).OrderByDescending(m => m.Fecha).Take(50).ToListAsync();
        ViewBag.Proveedores = await Db.ProveedorProductos.Include(pp => pp.Proveedor).Where(pp => pp.ProductoId == id).ToListAsync();
        return View(p);
    }

    [Authorize(Roles = Roles.AdminInventario)]
    public async Task<IActionResult> Crear()
    {
        await CargarListas();
        return View("Form", new Producto());
    }

    [Authorize(Roles = Roles.AdminInventario), HttpPost]
    public async Task<IActionResult> Crear(Producto p)
    {
        if (await Db.Productos.AnyAsync(x => x.Codigo == p.Codigo)) ModelState.AddModelError(nameof(p.Codigo), "Ya existe un producto con ese código.");
        if (!ModelState.IsValid) { await CargarListas(); return View("Form", p); }
        var stockInicial = p.StockActual;
        p.StockActual = 0;
        Db.Productos.Add(p);
        await Db.SaveChangesAsync();
        if (stockInicial > 0) _inv.Registrar(p, TiposMovimiento.Entrada, stockInicial, "Stock inicial", null, UsuarioNombre);
        Log.Registrar("Inventario", "Producto creado", $"{p.Codigo} - {p.Nombre}");
        await Db.SaveChangesAsync();
        Exito("Producto creado");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.AdminInventario)]
    public async Task<IActionResult> Editar(int id)
    {
        var p = await Db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        await CargarListas();
        return View("Form", p);
    }

    [Authorize(Roles = Roles.AdminInventario), HttpPost]
    public async Task<IActionResult> Editar(int id, Producto form)
    {
        var p = await Db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        if (await Db.Productos.AnyAsync(x => x.Codigo == form.Codigo && x.Id != id)) ModelState.AddModelError(nameof(form.Codigo), "Ya existe un producto con ese código.");
        if (!ModelState.IsValid) { await CargarListas(); form.Id = id; return View("Form", form); }
        // El stock solo cambia con movimientos, por eso no se copia aquí.
        p.Codigo = form.Codigo; p.Nombre = form.Nombre; p.Descripcion = form.Descripcion; p.CategoriaId = form.CategoriaId;
        p.BodegaId = form.BodegaId; p.PrecioCompra = form.PrecioCompra; p.PrecioVenta = form.PrecioVenta;
        p.StockMinimo = form.StockMinimo; p.Unidad = form.Unidad; p.Activo = form.Activo;
        Log.Registrar("Inventario", "Producto editado", $"{p.Codigo} - {p.Nombre}");
        await Db.SaveChangesAsync();
        Exito("Cambios guardados");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [Authorize(Roles = Roles.AdminInventario)]
    public async Task<IActionResult> Movimiento(int id, string tipo = TiposMovimiento.Entrada)
    {
        var p = await Db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        ViewBag.Producto = p;
        return View(new MovimientoVM { ProductoId = id, Tipo = tipo, Cantidad = tipo == TiposMovimiento.Ajuste ? p.StockActual : 1 });
    }

    [Authorize(Roles = Roles.AdminInventario), HttpPost]
    public async Task<IActionResult> Movimiento(MovimientoVM vm)
    {
        var p = await Db.Productos.FindAsync(vm.ProductoId);
        if (p == null) return NotFound();
        if (!ModelState.IsValid) { ViewBag.Producto = p; return View(vm); }
        try
        {
            var mov = _inv.Registrar(p, vm.Tipo, vm.Cantidad, vm.Motivo, vm.Referencia, UsuarioNombre);
            Log.Registrar("Inventario", $"Movimiento: {vm.Tipo}", $"{p.Nombre}: {mov.StockAnterior} → {mov.StockResultante}");
            await Db.SaveChangesAsync();
            Exito($"Movimiento registrado. Stock actual: {p.StockActual}");
            return RedirectToAction(nameof(Detalle), new { id = p.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Producto = p;
            return View(vm);
        }
    }

    [Authorize(Roles = Roles.AdminInventario), HttpPost]
    public async Task<IActionResult> Desactivar(int id)
    {
        var p = await Db.Productos.FindAsync(id);
        if (p == null) return NotFound();
        p.Activo = !p.Activo;
        Log.Registrar("Inventario", p.Activo ? "Producto activado" : "Producto desactivado", p.Nombre);
        await Db.SaveChangesAsync();
        Exito(p.Activo ? "Producto activado" : "Producto desactivado");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private async Task CargarListas()
    {
        ViewBag.Categorias = new SelectList(await Db.Categorias.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Bodegas = new SelectList(await Db.Bodegas.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.Unidades = Lista(Listas.Unidades);
    }
}

[Authorize(Roles = Roles.AdminInventario)]
public class CatalogosController : BaseController
{
    public CatalogosController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index()
    {
        ViewBag.Bodegas = await Db.Bodegas.Include(b => b.Productos).OrderBy(b => b.Nombre).ToListAsync();
        return View(await Db.Categorias.Include(c => c.Productos).OrderBy(c => c.Nombre).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> CrearCategoria(string nombre, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(nombre)) { Error("Escribe el nombre de la categoría"); return RedirectToAction(nameof(Index)); }
        Db.Categorias.Add(new Categoria { Nombre = nombre.Trim(), Descripcion = descripcion });
        Log.Registrar("Inventario", "Categoría creada", nombre);
        await Db.SaveChangesAsync();
        Exito("Categoría creada");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> EliminarCategoria(int id)
    {
        var c = await Db.Categorias.Include(x => x.Productos).FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();
        if (c.Productos.Any()) { Error("No se puede eliminar: la categoría tiene productos"); return RedirectToAction(nameof(Index)); }
        Db.Categorias.Remove(c);
        Log.Registrar("Inventario", "Categoría eliminada", c.Nombre);
        await Db.SaveChangesAsync();
        Exito("Categoría eliminada");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CrearBodega(string nombre, string? ubicacion)
    {
        if (string.IsNullOrWhiteSpace(nombre)) { Error("Escribe el nombre de la bodega"); return RedirectToAction(nameof(Index)); }
        Db.Bodegas.Add(new Bodega { Nombre = nombre.Trim(), Ubicacion = ubicacion });
        Log.Registrar("Inventario", "Bodega creada", nombre);
        await Db.SaveChangesAsync();
        Exito("Bodega creada");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> EliminarBodega(int id)
    {
        var b = await Db.Bodegas.Include(x => x.Productos).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        if (b.Productos.Any()) { Error("No se puede eliminar: la bodega tiene productos"); return RedirectToAction(nameof(Index)); }
        Db.Bodegas.Remove(b);
        Log.Registrar("Inventario", "Bodega eliminada", b.Nombre);
        await Db.SaveChangesAsync();
        Exito("Bodega eliminada");
        return RedirectToAction(nameof(Index));
    }
}

public class MovimientosController : BaseController
{
    public MovimientosController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(string? tipo, DateTime? desde, DateTime? hasta)
    {
        var d = desde ?? DateTime.Today.AddDays(-30);
        var h = (hasta ?? DateTime.Today).Date.AddDays(1);
        var q = Db.Movimientos.Include(m => m.Producto).Where(m => m.Fecha >= d && m.Fecha < h);
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(m => m.Tipo == tipo);
        ViewBag.Tipo = tipo; ViewBag.Desde = d; ViewBag.Hasta = h.AddDays(-1);
        return View(await q.OrderByDescending(m => m.Fecha).Take(500).ToListAsync());
    }
}
