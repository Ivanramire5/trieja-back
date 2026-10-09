using Microsoft.AspNetCore.Mvc;
using InventarioSaaS.Api.Data;
using InventarioSaaS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; // NUEVO: Importamos seguridad

namespace InventarioSaaS.Api.Controllers
{
    [Authorize] // NUEVO: Candado maestro para todo el controlador
    [Route("api/[controller]")]
    [ApiController]
    public class TransaccionesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransaccionesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // NUEVO: Método privado para extraer el ID del negocio desde el Token
        private int ObtenerTenantIdSeguro()
        {
            var tenantClaim = User.FindFirst("TenantId");
            if (tenantClaim == null) throw new UnauthorizedAccessException("Token inválido");
            
            return int.Parse(tenantClaim.Value);
        }

        public class RegistroVentaDto
        {
            public int ProductoId { get; set; }
            public int Cantidad { get; set; }
            public decimal TotalCobrado { get; set; }
            public string TipoOperacion { get; set; } = string.Empty;
            public DateTime Fecha { get; set; }
            // Nota: Podrías eliminar TenantId del DTO si quieres, ya no le haremos caso a lo que mande React.
            public int TenantId { get; set; } 
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarTransaccion(RegistroVentaDto venta)
        {
            int tenantIdReal = ObtenerTenantIdSeguro(); // Extraemos del Token

            var movimiento = new Movimiento
            {
                TenantId = tenantIdReal, // Blindado
                ProductoId = venta.ProductoId,
                Tipo = "Salida", 
                Cantidad = venta.Cantidad,
                Motivo = venta.TipoOperacion,
                Fecha = venta.Fecha.ToUniversalTime() 
            };
            
            _context.Set<Movimiento>().Add(movimiento); 

            var ingresoCaja = new TransaccionCaja
            {
                TenantId = tenantIdReal, // Blindado
                Tipo = "Ingreso", 
                Monto = venta.TotalCobrado,
                Descripcion = $"Venta de {venta.Cantidad} unidades (Prod. ID: {venta.ProductoId})",
                Fecha = venta.Fecha.ToUniversalTime()
            };

            _context.Set<TransaccionCaja>().Add(ingresoCaja);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Venta registrada con éxito en Caja y Movimientos." });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTransaccionesCaja()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var transacciones = await _context.Set<TransaccionCaja>()
                .Where(t => t.TenantId == tenantIdReal) // Filtramos por negocio
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();
            
            return Ok(transacciones);
        }

        [HttpGet("movimientos")]
        public async Task<IActionResult> ObtenerMovimientos()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var movimientos = await _context.Set<Movimiento>()
                .Where(m => m.TenantId == tenantIdReal) // Filtramos por negocio
                .Include(m => m.Producto)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
            
            return Ok(movimientos);
        }

        [HttpPost("cerrar-turno")]
        public async Task<IActionResult> CerrarTurno()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var cierre = new TransaccionCaja
            {
                TenantId = tenantIdReal, // Blindado
                Tipo = "Cierre", 
                Monto = 0,
                Descripcion = "Cierre de Caja del Turno",
                Fecha = DateTime.UtcNow
            };

            _context.Set<TransaccionCaja>().Add(cierre);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Caja cerrada correctamente" });
        }

        [HttpGet("movimientos/turno-actual")]
        public async Task<IActionResult> ObtenerMovimientosTurnoActual()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var ultimoCierre = await _context.Set<TransaccionCaja>()
                .Where(t => t.TenantId == tenantIdReal && t.Tipo == "Cierre") // Filtramos por negocio
                .OrderByDescending(t => t.Fecha)
                .FirstOrDefaultAsync();

            var fechaInicio = ultimoCierre != null ? ultimoCierre.Fecha : DateTime.MinValue;

            var movimientos = await _context.Set<Movimiento>()
                .Include(m => m.Producto)
                .Where(m => m.TenantId == tenantIdReal && m.Fecha > fechaInicio) // Filtramos por negocio
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
            
            return Ok(movimientos);
        }

        [HttpDelete("movimientos/{id}")]
        public async Task<IActionResult> EliminarMovimiento(int id)
        {
            int tenantIdReal = ObtenerTenantIdSeguro();
            var movimiento = await _context.Set<Movimiento>().FindAsync(id);
            
            if (movimiento == null) return NotFound();
            
            // Seguridad extra: Evitar que borren un movimiento de otro negocio
            if (movimiento.TenantId != tenantIdReal) return Forbid(); 

            _context.Set<Movimiento>().Remove(movimiento);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        [HttpDelete("movimientos/limpiar")]
        public async Task<IActionResult> LimpiarHistorial()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var ventas = await _context.Set<Movimiento>()
                .Where(m => m.TenantId == tenantIdReal && m.Tipo == "Salida") // Filtramos por negocio
                .ToListAsync();
            
            _context.Set<Movimiento>().RemoveRange(ventas);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("resumen-diario")]
        public async Task<IActionResult> ObtenerResumenDiario()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();

            var ingresos = await _context.Set<TransaccionCaja>()
                .Where(t => t.TenantId == tenantIdReal && t.Tipo == "Ingreso") // Filtramos por negocio
                .ToListAsync();

            var resumen = ingresos
                .GroupBy(t => t.Fecha.ToLocalTime().Date)
                .Select(g => new {
                    Fecha = g.Key.ToString("yyyy-MM-dd"),
                    Total = g.Sum(t => t.Monto),
                    Operaciones = g.Count() 
                })
                .OrderByDescending(r => r.Fecha) 
                .ToList();

            return Ok(resumen);
        }
    }
}