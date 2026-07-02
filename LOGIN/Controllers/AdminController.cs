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

        // Validación interna para asegurar que solo el Admin entre aquí
        private bool EsAdmin()
        {
            return HttpContext.Session.GetString("UsuarioRol") == "Admin";
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            // 1. Obtener el Historial General de Pedidos (Maestro-Detalle)
            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalles!)
                    .ThenInclude(d => d.Producto)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            // 2. Preparar datos para Gráfico 1: Ventas de los últimos 6 meses
            var fechaInicio = DateTime.Now.AddMonths(-5);
            fechaInicio = new DateTime(fechaInicio.Year, fechaInicio.Month, 1);

            var ventasMensuales = await _context.Pedidos
                .Where(p => p.FechaPedido >= fechaInicio && p.Estado != EstadoPedido.Cancelado)
                .GroupBy(p => new { p.FechaPedido.Year, p.FechaPedido.Month })
                .Select(g => new {
                    Mes = g.Key.Month,
                    Total = g.Sum(p => p.Total)
                })
                .ToListAsync();

            string[] nombresMeses = { "", "Ene", "Feb", "Mar", "Abr", "May", "Jun", "Jul", "Ago", "Sep", "Oct", "Nov", "Dic" };
            var labelsMeses = new List<string>();
            var dataVentas = new List<decimal>();

            for (int i = 0; i < 6; i++)
            {
                var mesActual = fechaInicio.AddMonths(i);
                labelsMeses.Add(nombresMeses[mesActual.Month]);
                var ventaMes = ventasMensuales.FirstOrDefault(v => v.Mes == mesActual.Month);
                dataVentas.Add(ventaMes != null ? ventaMes.Total : 0);
            }

            ViewBag.LabelsMeses = labelsMeses;
            ViewBag.DataVentas = dataVentas;

            // 3. Preparar datos para Gráfico 2: Top 5 Fideos más vendidos
            var topProductos = await _context.PedidoDetalles
                .Include(d => d.Producto)
                .GroupBy(d => d.Producto!.Nombre)
                .Select(g => new {
                    Producto = g.Key,
                    CantidadVendida = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(g => g.CantidadVendida)
                .Take(5)
                .ToListAsync();

            ViewBag.LabelsProductos = topProductos.Select(p => p.Producto).ToList();
            ViewBag.DataProductos = topProductos.Select(p => p.CantidadVendida).ToList();

            // 4. Tarjetas KPI de Resumen Rápido
            ViewBag.TotalIngresos = pedidos.Where(p => p.Estado != EstadoPedido.Cancelado).Sum(p => p.Total);
            ViewBag.TotalPedidos = pedidos.Count;
            ViewBag.TotalClientes = await _context.Usuarios.CountAsync(u => u.Rol == "Cliente");

            return View(pedidos);
        }

        // Método para que el Admin actualice el estado del pedido desde la tabla
        [HttpPost]
        public async Task<IActionResult> ActualizarEstado(int id, EstadoPedido nuevoEstado)
        {
            if (!EsAdmin()) return RedirectToAction("Login", "Account");

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido != null)
            {
                pedido.Estado = nuevoEstado;
                await _context.SaveChangesAsync();
                TempData["Mensaje"] = $"El pedido REF-{pedido.NumeroReferencia} fue actualizado a '{nuevoEstado}'.";
            }
            
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
