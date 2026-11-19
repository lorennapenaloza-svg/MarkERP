using System.Text;
using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

[Authorize(Roles = Roles.AdminTH)]
public class EmpleadosController : BaseController
{
    public EmpleadosController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(string? q, string? area, string? eps, bool inactivos = false)
    {
        var query = Db.Empleados.Where(e => e.Activo != inactivos);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(e => (e.Nombres + " " + e.Apellidos).ToLower().Contains(q.ToLower()) || e.NumeroDocumento.Contains(q) || e.Cargo.ToLower().Contains(q.ToLower()));
        if (!string.IsNullOrEmpty(area)) query = query.Where(e => e.Area == area);
        if (!string.IsNullOrEmpty(eps)) query = query.Where(e => e.Eps == eps);
        ViewBag.Q = q; ViewBag.Area = area; ViewBag.EpsSel = eps; ViewBag.Inactivos = inactivos;
        return View(await query.OrderBy(e => e.Nombres).ToListAsync());
    }

    public async Task<IActionResult> Detalle(int id)
    {
        var e = await Db.Empleados.FirstOrDefaultAsync(x => x.Id == id);
        if (e == null) return NotFound();
        ViewBag.Turnos = await Db.Turnos.Where(t => t.EmpleadoId == id && t.Fecha >= DateTime.Today.AddDays(-7)).OrderBy(t => t.Fecha).ToListAsync();
        ViewBag.Novedades = await Db.Novedades.Where(n => n.EmpleadoId == id).OrderByDescending(n => n.FechaInicio).ToListAsync();
        return View(e);
    }

    public IActionResult Crear()
    {
        CargarListas();
        return View("Form", new Empleado());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Empleado e)
    {
        Validar(e);
        if (await Db.Empleados.AnyAsync(x => x.NumeroDocumento == e.NumeroDocumento)) ModelState.AddModelError(nameof(e.NumeroDocumento), "Ya existe un empleado con ese documento.");
        if (!ModelState.IsValid) { CargarListas(); return View("Form", e); }
        Db.Empleados.Add(e);
        Log.Registrar("Talento humano", "Empleado creado", e.NombreCompleto);
        await Db.SaveChangesAsync();
        Exito("Empleado registrado");
        return RedirectToAction(nameof(Detalle), new { id = e.Id });
    }

    public async Task<IActionResult> Editar(int id)
    {
        var e = await Db.Empleados.FindAsync(id);
        if (e == null) return NotFound();
        CargarListas();
        return View("Form", e);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, Empleado form)
    {
        var e = await Db.Empleados.FindAsync(id);
        if (e == null) return NotFound();
        Validar(form);
        if (await Db.Empleados.AnyAsync(x => x.NumeroDocumento == form.NumeroDocumento && x.Id != id)) ModelState.AddModelError(nameof(form.NumeroDocumento), "Ya existe un empleado con ese documento.");
        if (!ModelState.IsValid) { CargarListas(); form.Id = id; return View("Form", form); }
        form.Id = id;
        Db.Entry(e).CurrentValues.SetValues(form);
        Log.Registrar("Talento humano", "Ficha de empleado actualizada", e.NombreCompleto);
        await Db.SaveChangesAsync();
        Exito("Ficha actualizada");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var e = await Db.Empleados.FindAsync(id);
        if (e == null) return NotFound();
        e.Activo = !e.Activo;
        e.FechaRetiro = e.Activo ? null : DateTime.Today;
        Log.Registrar("Talento humano", e.Activo ? "Empleado reintegrado" : "Empleado retirado", e.NombreCompleto);
        await Db.SaveChangesAsync();
        Exito(e.Activo ? "Empleado reintegrado" : "Empleado marcado como retirado");
        return RedirectToAction(nameof(Detalle), new { id });
    }

    public async Task<IActionResult> Exportar()
    {
        var lista = await Db.Empleados.OrderBy(e => e.Apellidos).ToListAsync();
        var sb = new StringBuilder();
        sb.AppendLine("Documento;Nombres;Apellidos;Edad;Sexo;Estatura cm;Peso kg;RH;EPS;ARL;Pensión;Cesantías;Caja;Cargo;Área;Contrato;Ingreso;Antigüedad;Salario;Teléfono;Correo;Activo");
        foreach (var e in lista)
            sb.AppendLine(string.Join(";", $"{e.TipoDocumento} {e.NumeroDocumento}", e.Nombres, e.Apellidos, e.Edad, e.Sexo, e.EstaturaCm, e.PesoKg,
                e.TipoSangre, e.Eps, e.Arl, e.FondoPensiones, e.FondoCesantias, e.CajaCompensacion, e.Cargo, e.Area, e.TipoContrato,
                e.FechaIngreso.ToString("yyyy-MM-dd"), e.Antiguedad, e.Salario, e.Telefono, e.Email, e.Activo ? "Sí" : "No"));
        return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv", "empleados_markerp.csv");
    }

    private void Validar(Empleado e)
    {
        if (e.FechaNacimiento > DateTime.Today.AddYears(-14)) ModelState.AddModelError(nameof(e.FechaNacimiento), "Revisa la fecha de nacimiento.");
        if (e.FechaRetiro.HasValue && e.FechaRetiro < e.FechaIngreso) ModelState.AddModelError(nameof(e.FechaRetiro), "La fecha de retiro no puede ser anterior al ingreso.");
        // Las colecciones no vienen en el formulario.
        ModelState.Remove(nameof(Empleado.Turnos));
        ModelState.Remove(nameof(Empleado.Novedades));
    }

    private void CargarListas()
    {
        ViewBag.TiposDoc = Lista(Listas.TiposDocumento);
        ViewBag.Sexos = Lista(Listas.Sexos);
        ViewBag.EstadosCiviles = Lista(Listas.EstadosCiviles);
        ViewBag.Sangre = Lista(Listas.TiposSangre);
        ViewBag.Eps = Lista(Listas.Eps);
        ViewBag.Arl = Lista(Listas.Arl);
        ViewBag.Pensiones = Lista(Listas.FondosPension);
        ViewBag.Cesantias = Lista(Listas.FondosCesantias);
        ViewBag.Cajas = Lista(Listas.Cajas);
        ViewBag.Contratos = Lista(Listas.TiposContrato);
        ViewBag.Jornadas = Lista(Listas.Jornadas);
        ViewBag.Areas = Lista(Listas.Areas);
        ViewBag.Niveles = Lista(Listas.NivelesEducativos);
        ViewBag.Bancos = Lista(Listas.Bancos);
        ViewBag.TiposCuenta = Lista(Listas.TiposCuenta);
    }
}

[Authorize(Roles = Roles.AdminTH)]
public class TurnosController : BaseController
{
    public TurnosController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(DateTime? semana)
    {
        var baseDia = (semana ?? DateTime.Today).Date;
        var lunes = baseDia.AddDays(-(((int)baseDia.DayOfWeek + 6) % 7));
        var domingo = lunes.AddDays(7);
        ViewBag.Lunes = lunes;
        ViewBag.Empleados = await Db.Empleados.Where(e => e.Activo).OrderBy(e => e.Nombres).ToListAsync();
        return View(await Db.Turnos.Include(t => t.Empleado).Where(t => t.Fecha >= lunes && t.Fecha < domingo).ToListAsync());
    }

    public async Task<IActionResult> Crear(int? empleadoId, DateTime? fecha)
    {
        await CargarListas(empleadoId);
        return View("Form", new Turno { EmpleadoId = empleadoId ?? 0, Fecha = fecha ?? DateTime.Today });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Turno t)
    {
        ModelState.Remove(nameof(Turno.Empleado));
        if (t.HoraFin <= t.HoraInicio) ModelState.AddModelError(nameof(t.HoraFin), "La hora de fin debe ser posterior a la de inicio.");
        if (t.EmpleadoId <= 0) ModelState.AddModelError(nameof(t.EmpleadoId), "Elige un empleado.");
        if (!ModelState.IsValid) { await CargarListas(t.EmpleadoId); return View("Form", t); }
        Db.Turnos.Add(t);
        Log.Registrar("Talento humano", "Turno asignado", $"Empleado {t.EmpleadoId} - {t.Fecha:dd/MM}");
        await Db.SaveChangesAsync();
        Exito("Turno asignado");
        return RedirectToAction(nameof(Index), new { semana = t.Fecha.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id)
    {
        var t = await Db.Turnos.FindAsync(id);
        if (t == null) return NotFound();
        Db.Turnos.Remove(t);
        await Db.SaveChangesAsync();
        Exito("Turno eliminado");
        return RedirectToAction(nameof(Index), new { semana = t.Fecha.ToString("yyyy-MM-dd") });
    }

    private async Task CargarListas(int? empleadoId)
    {
        ViewBag.Empleados = new SelectList((await Db.Empleados.Where(e => e.Activo).OrderBy(e => e.Nombres).ToListAsync())
            .Select(e => new { e.Id, Nombre = e.NombreCompleto }), "Id", "Nombre", empleadoId);
        ViewBag.Tipos = Lista(Listas.TiposTurno);
    }
}

[Authorize(Roles = Roles.AdminTH)]
public class NovedadesController : BaseController
{
    public NovedadesController(AppDbContext db, BitacoraService log) : base(db, log) { }

    public async Task<IActionResult> Index(string? tipo, string? estado)
    {
        var q = Db.Novedades.Include(n => n.Empleado).AsQueryable();
        if (!string.IsNullOrEmpty(tipo)) q = q.Where(n => n.Tipo == tipo);
        if (!string.IsNullOrEmpty(estado)) q = q.Where(n => n.Estado == estado);
        ViewBag.Tipo = tipo; ViewBag.EstadoSel = estado;
        return View(await q.OrderByDescending(n => n.FechaInicio).ToListAsync());
    }

    public async Task<IActionResult> Crear(int? empleadoId)
    {
        await CargarListas(empleadoId);
        return View("Form", new Novedad { EmpleadoId = empleadoId ?? 0 });
    }

    [HttpPost]
    public async Task<IActionResult> Crear(Novedad n)
    {
        ModelState.Remove(nameof(Novedad.Empleado));
        if (n.FechaFin < n.FechaInicio) ModelState.AddModelError(nameof(n.FechaFin), "La fecha de fin no puede ser anterior al inicio.");
        if (n.EmpleadoId <= 0) ModelState.AddModelError(nameof(n.EmpleadoId), "Elige un empleado.");
        if (!ModelState.IsValid) { await CargarListas(n.EmpleadoId); return View("Form", n); }
        Db.Novedades.Add(n);
        Log.Registrar("Talento humano", $"Novedad registrada: {n.Tipo}", $"Empleado {n.EmpleadoId}");
        await Db.SaveChangesAsync();
        Exito("Novedad registrada");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(int id, string estado)
    {
        var n = await Db.Novedades.FindAsync(id);
        if (n == null) return NotFound();
        if (!Listas.EstadosNovedad.Contains(estado)) return BadRequest();
        n.Estado = estado;
        Log.Registrar("Talento humano", $"Novedad {estado.ToLower()}", $"#{n.Id}");
        await Db.SaveChangesAsync();
        Exito($"Novedad {estado.ToLower()}");
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarListas(int? empleadoId)
    {
        ViewBag.Empleados = new SelectList((await Db.Empleados.Where(e => e.Activo).OrderBy(e => e.Nombres).ToListAsync())
            .Select(e => new { e.Id, Nombre = e.NombreCompleto }), "Id", "Nombre", empleadoId);
        ViewBag.Tipos = Lista(Listas.TiposNovedad);
        ViewBag.Estados = Lista(Listas.EstadosNovedad);
    }
}
