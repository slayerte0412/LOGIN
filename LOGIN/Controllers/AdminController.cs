using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;
using LOGIN.Models;

namespace LOGIN.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> HistorialVentas(int? mes)
        {
            // Verificamos sesión y rol original
            var rol = HttpContext.Session.GetString("UsuarioRol");
            if (rol != "Administrador")
            {
                return RedirectToAction("Index", "Tienda");
            }

            var pedidos = _context.Pedidos.Include(p => p.Usuario).AsQueryable();

            if (mes.HasValue && mes.Value > 0 && mes.Value <= 12)
            {
                pedidos = pedidos.Where(p => p.FechaPedido.Month == mes.Value);
            }

            var listaPedidos = await pedidos.OrderByDescending(p => p.FechaPedido).ToListAsync();

            ViewBag.TotalVentas = listaPedidos.Sum(p => p.Total);
            ViewBag.MesSeleccionado = mes;

            int pendientes = listaPedidos.Count(p => p.Estado == 0);
            int completados = listaPedidos.Count(p => p.Estado == 1);
            int cancelados = listaPedidos.Count(p => p.Estado == 2);

            ViewBag.EstadosChartData = $"[{pendientes}, {completados}, {cancelados}]";

            return View(listaPedidos);
        }
    }
}