using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var hace7 = hoy.AddDays(-6);

        // Las sumas se hacen en memoria (SQLite no suma decimales en SQL).
        var ventas = await Db.Ventas.Where(v => v.Estado == EstadosVenta.Pagada && v.Fecha >= inicioMesAnterior).ToListAsync();
        var productos = await Db.Productos.Where(p => p.Activo).Include(p => p.Categoria).ToListAsync();
        var ahora = DateTime.Now.TimeOfDay;
        var turnosHoy = await Db.Turnos.Where(t => t.Fecha == hoy).ToListAsync();

        var detalles7 = await Db.DetallesVenta.Include(d => d.Producto).Include(d => d.Venta)
            .Where(d => d.Venta!.Fecha >= hace7 && d.Venta.Estado == EstadosVenta.Pagada).ToListAsync();

        var vm = new DashboardVM
        {
            VentasHoy = ventas.Where(v => v.Fecha >= hoy).Sum(v => v.Total),
            NumVentasHoy = ventas.Count(v => v.Fecha >= hoy),
            VentasMes = ventas.Where(v => v.Fecha >= inicioMes).Sum(v => v.Total),
            VentasMesAnterior = ventas.Where(v => v.Fecha < inicioMes).Sum(v => v.Total),
            TotalProductos = productos.Count,
            ValorInventario = productos.Sum(p => p.ValorInventario),
            StockBajo = productos.Where(p => p.StockBajo).OrderBy(p => p.StockActual).Take(6).ToList(),
            CotizacionesAbiertas = await Db.Solicitudes.CountAsync(s => s.Estado == EstadosSolicitud.Enviada || s.Estado == EstadosSolicitud.EnEvaluacion),
            OrdenesPendientes = await Db.OrdenesCompra.CountAsync(o => o.Estado == EstadosOrden.Borrador || o.Estado == EstadosOrden.Enviada),
            EmpleadosActivos = await Db.Empleados.CountAsync(e => e.Activo),
            EmpleadosEnTurno = turnosHoy.Where(t => t.HoraInicio <= ahora && t.HoraFin >= ahora).Select(t => t.EmpleadoId).Distinct().Count(),
            UltimasVentas = await Db.Ventas.Include(v => v.Cliente).OrderByDescending(v => v.Fecha).Take(6).ToListAsync(),
            NovedadesActivas = await Db.Novedades.Include(n => n.Empleado)
                .Where(n => n.FechaFin >= hoy && n.Estado != "Rechazada").OrderBy(n => n.FechaInicio).Take(5).ToListAsync(),
            TopProductos = detalles7.GroupBy(d => d.Producto!.Nombre)
                .Select(g => (g.Key, g.Sum(d => d.Cantidad))).OrderByDescending(x => x.Item2).Take(5).ToList()
        };
        for (var i = 6; i >= 0; i--)
        {
            var d = hoy.AddDays(-i);
            vm.EtiquetasDias.Add(d.ToString("ddd d", Calculos.Co));
            vm.VentasDias.Add(ventas.Where(v => v.Fecha.Date == d).Sum(v => v.Total));
        }
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
