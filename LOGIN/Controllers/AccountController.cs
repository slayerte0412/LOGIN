using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Models;
using LOGIN.Data;

namespace LOGIN.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                return RedirectToAction("Index", "Home");
            }

            // Guardamos la URL a la que el usuario intentaba ir (ej: /Tienda/Checkout)
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

                if (usuario != null)
                {
                    // Guardamos los datos de sesión, incluyendo el Rol
                    HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
                    HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre ?? "");
                    HttpContext.Session.SetString("UsuarioEmail", usuario.Email ?? "");
                    HttpContext.Session.SetString("UsuarioRol", usuario.Rol ?? "Cliente");

                    // Si venía de intentar pagar en el carrito, lo devolvemos ahí
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Email o contraseña incorrectos");
                }
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                var existe = await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email);
                if (existe)
                {
                    ModelState.AddModelError("Email", "Este email ya está registrado");
                    return View(usuario);
                }

                // Aseguramos que los nuevos registros sean Clientes por defecto
                usuario.Rol = "Cliente";
                usuario.FechaRegistro = DateTime.Now;

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                TempData["Mensaje"] = "Registro exitoso. ¡Inicia sesión!";
                return RedirectToAction("Login");
            }
            return View(usuario);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Tienda");
        }
    }
}