using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;
using System.Text.Json;

namespace LOGIN.Controllers
{
    public class TiendaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TiendaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Clase auxiliar para el carrito anónimo en sesión
        public class CarritoAnonimoItem
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
        }

        public async Task<IActionResult> Index()
        {
            var productos = await _context.Productos.Where(p => p.Activo).ToListAsync();
            ViewBag.CarritoCount = ObtenerCantidadCarrito();
            return View(productos);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAlCarrito(int productoId, int cantidad)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null || producto.Cantidad < cantidad)
            {
                return Json(new { success = false, message = "Producto no disponible o stock insuficiente." });
            }

            var userIdString = HttpContext.Session.GetString("UsuarioId");

            if (string.IsNullOrEmpty(userIdString))
            {
                // Usuario NO logueado: Guardar en Sesión (JSON)
                var carritoJson = HttpContext.Session.GetString("CarritoAnonimo");
                var carrito = string.IsNullOrEmpty(carritoJson) ? new List<CarritoAnonimoItem>()
                    : JsonSerializer.Deserialize<List<CarritoAnonimoItem>>(carritoJson);

                var itemExistente = carrito.FirstOrDefault(c => c.ProductoId == productoId);
                if (itemExistente != null)
                {
                    itemExistente.Cantidad += cantidad;
                }
                else
                {
                    carrito.Add(new CarritoAnonimoItem { ProductoId = productoId, Cantidad = cantidad });
                }

                HttpContext.Session.SetString("CarritoAnonimo", JsonSerializer.Serialize(carrito));
            }
            else
            {
                // Usuario Logueado: Guardar en Base de Datos
                int usuarioId = int.Parse(userIdString);
                var carritoItem = await _context.CarritoItems
                    .FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.ProductoId == productoId);

                if (carritoItem != null)
                {
                    carritoItem.Cantidad += cantidad;
                }
                else
                {
                    _context.CarritoItems.Add(new CarritoItem
                    {
                        UsuarioId = usuarioId,
                        ProductoId = productoId,
                        Cantidad = cantidad
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Producto agregado al carrito." });
        }

        // NUEVO: Método Asíncrono para sumar/restar sin recargar la página
        [HttpPost]
        public async Task<IActionResult> ActualizarCantidadCarrito(int productoId, string operacion)
        {
            var userIdString = HttpContext.Session.GetString("UsuarioId");
            int nuevaCantidad = 0;
            decimal precioUnitario = 0;

            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) return Json(new { success = false });
            precioUnitario = producto.Precio;

            if (string.IsNullOrEmpty(userIdString))
            {
                // Lógica para usuario anónimo
                var carritoJson = HttpContext.Session.GetString("CarritoAnonimo");
                if (string.IsNullOrEmpty(carritoJson)) return Json(new { success = false });

                var carrito = JsonSerializer.Deserialize<List<CarritoAnonimoItem>>(carritoJson);
                var item = carrito.FirstOrDefault(c => c.ProductoId == productoId);
                if (item == null) return Json(new { success = false });

                if (operacion == "sumar" && item.Cantidad < producto.Cantidad) item.Cantidad++;
                else if (operacion == "restar" && item.Cantidad > 1) item.Cantidad--;

                nuevaCantidad = item.Cantidad;
                HttpContext.Session.SetString("CarritoAnonimo", JsonSerializer.Serialize(carrito));
            }
            else
            {
                // Lógica para usuario logueado
                int usuarioId = int.Parse(userIdString);
                var item = await _context.CarritoItems.FirstOrDefaultAsync(c => c.UsuarioId == usuarioId && c.ProductoId == productoId);
                if (item == null) return Json(new { success = false });

                if (operacion == "sumar" && item.Cantidad < producto.Cantidad) item.Cantidad++;
                else if (operacion == "restar" && item.Cantidad > 1) item.Cantidad--;

                nuevaCantidad = item.Cantidad;
                await _context.SaveChangesAsync();
            }

            decimal nuevoSubtotal = nuevaCantidad * precioUnitario;

            // Retornamos los datos formateados para que JavaScript los actualice visualmente
            return Json(new
            {
                success = true,
                cantidad = nuevaCantidad,
                subtotalFormateado = "Bs. " + nuevoSubtotal.ToString("0.00")
            });
        }

        // Este es el paso clave: Redirigir al login solo cuando hacen clic en "Pagar"
        [HttpGet]
        public IActionResult Checkout()
        {
            var userIdString = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(userIdString))
            {
                // Mandamos al login, pero le decimos que regrese a /Tienda/Checkout al terminar
                return RedirectToAction("Login", "Account", new { returnUrl = "/Tienda/Checkout" });
            }

            // (Aquí irá la lógica para migrar el carrito anónimo a la DB y mostrar la vista de pago)
            return View();
        }

        private int ObtenerCantidadCarrito()
        {
            var userIdString = HttpContext.Session.GetString("UsuarioId");
            if (string.IsNullOrEmpty(userIdString))
            {
                var carritoJson = HttpContext.Session.GetString("CarritoAnonimo");
                if (string.IsNullOrEmpty(carritoJson)) return 0;
                var carrito = JsonSerializer.Deserialize<List<CarritoAnonimoItem>>(carritoJson);
                return carrito.Sum(c => c.Cantidad);
            }
            else
            {
                int usuarioId = int.Parse(userIdString);
                return _context.CarritoItems.Where(c => c.UsuarioId == usuarioId).Sum(c => c.Cantidad);
            }
        }
    }
}