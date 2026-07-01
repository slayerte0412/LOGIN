using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LOGIN.Data;

namespace LOGIN.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context) { _context = context; }
        public async Task<IActionResult> HistorialVentas(int? mes)
        {
            if (HttpContext.Session.GetString("UsuarioRol") != "Administrador") return RedirectToAction("Index", "Tienda");
            var pedidos = _context.Pedidos.Include(p => p.Usuario).AsQueryable();
            if (mes.HasValue) pedidos = pedidos.Where(p => p.FechaPedido.Month == mes.Value);
            var lista = await pedidos.OrderByDescending(p => p.FechaPedido).ToListAsync();
            ViewBag.TotalVentas = lista.Sum(p => p.Total);
            ViewBag.EstadosChartData = $"[{lista.Count(p => p.Estado == 0)}, {lista.Count(p => p.Estado == 1)}, {lista.Count(p => p.Estado == 2)}]";
            return View(lista);
        }
    }
}