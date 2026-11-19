using System.Security.Claims;
using MarkERP.Web.Data;
using MarkERP.Web.Models;
using MarkERP.Web.Services;
using MarkERP.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarkERP.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(AppDbContext db, BitacoraService log) : base(db, log) { }

    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginVM { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost]
    public async Task<IActionResult> Login(LoginVM vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var email = vm.Email.Trim().ToLower();
        var usuario = await Db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Activo);
        var ok = usuario != null &&
                 new PasswordHasher<Usuario>().VerifyHashedPassword(usuario, usuario.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;
        if (!ok)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nombre),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = vm.Recordarme });

        usuario.UltimoAcceso = DateTime.Now;
        Db.Bitacora.Add(new Bitacora { Usuario = usuario.Nombre, Modulo = "Acceso", Accion = "Inicio de sesión" });
        await Db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
