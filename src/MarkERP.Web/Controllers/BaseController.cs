using MarkERP.Web.Data;
using MarkERP.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly AppDbContext Db;
    protected readonly BitacoraService Log;
    protected BaseController(AppDbContext db, BitacoraService log) { Db = db; Log = log; }

    protected string UsuarioNombre => User.Nombre();

    protected void Exito(string mensaje) => TempData["ok"] = mensaje;
    protected void Error(string mensaje) => TempData["error"] = mensaje;

    protected async Task<SelectList> ProductosSelect(int? seleccionado = null) =>
        new(await Db.Productos.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync(), "Id", "Nombre", seleccionado);

    protected static SelectList Lista(IEnumerable<string> valores, string? seleccionado = null) => new(valores, seleccionado);
}
