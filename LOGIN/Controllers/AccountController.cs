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

        // Aseguramos la hora de Bolivia para los servidores en la nube
        private DateTime ObtenerHoraBolivia()
        {
            return DateTime.UtcNow.AddHours(-4);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UsuarioId")))
            {
                // Redirigir según el rol si ya está logueado
                if (HttpContext.Session.GetString("UsuarioRol") == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                return RedirectToAction("Index", "Tienda");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.Password == model.Password);

                if (usuario != null)
                {
                    HttpContext.Session.SetString("UsuarioId", usuario.Id.ToString());
                    HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre ?? "");
                    HttpContext.Session.SetString("UsuarioEmail", usuario.Email ?? "");
                    HttpContext.Session.SetString("UsuarioRol", usuario.Rol ?? "Cliente");
                    
                    // Redirección inteligente post-login
                    if (usuario.Rol == "Admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    return RedirectToAction("Index", "Tienda");
                }
                else
                {
                    ModelState.AddModelError("", "Email o contraseña incorrectos");
                }
            }
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

                if (usuario.Email.EndsWith("@lazaroni.com", StringComparison.OrdinalIgnoreCase))
                {
                    usuario.Rol = "Admin";
                }
                else
                {
                    usuario.Rol = "Cliente";
                }

                // Aplicamos la hora exacta de Bolivia
                usuario.FechaRegistro = ObtenerHoraBolivia();
                
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
            return RedirectToAction("Login");
        }
    }
}
                {
                    usuario.Rol = "Cliente";
                }

                // Aplicamos la hora exacta de Bolivia
                usuario.FechaRegistro = ObtenerHoraBolivia();
                
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
            return RedirectToAction("Login");
        }
    }
}
