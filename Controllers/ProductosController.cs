using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;
using InventarioSaaS.Api.Models;
using Microsoft.AspNetCore.Authorization;

namespace InventarioSaaS.Api.Controllers
{
    // Esto define la ruta base de este controlador. 
    // [controller] se reemplaza automáticamente por "Productos", por lo que la ruta será: /api/productos
    [Authorize] // <--- Candado activado
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // Inyección de dependencias: ASP.NET Core nos pasa el contexto de la base de datos automáticamente
        public ProductosController(ApplicationDbContext context)
        {
            _context = context;
        }
        private int ObtenerTenantIdSeguro()
    {
        var tenantClaim = User.FindFirst("TenantId");
        if (tenantClaim == null) throw new UnauthorizedAccessException("Token inválido");
        return int.Parse(tenantClaim.Value);
    }

        // GET: api/productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {int miTenantId = ObtenerTenantIdSeguro(); 

    // 2. Agregamos un .Where() para filtrar la búsqueda en PostgreSQL
        var productos = await _context.Productos
            .Where(p => p.TenantId == miTenantId)
            .ToListAsync();
            return Ok(productos);
        }

        // POST: api/productos
        [HttpPost]
        public async Task<ActionResult<Producto>> CrearProducto(Producto producto)
        {
            // 1. SEGURIDAD: Sobrescribimos el TenantId con el ID real del usuario conectado.
            // Así ignoramos por completo cualquier ID falso o erróneo que envíe React.
            producto.TenantId = ObtenerTenantIdSeguro();

            // 2. Agregamos el producto a la base de datos
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // 3. Retornamos un estado 201 Created y el producto recién creado
            return CreatedAtAction(nameof(GetProductos), new { id = producto.Id }, producto);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarProducto(int id, Producto producto)
        {
            // 1. Verificamos que el ID de la URL coincida con el ID del objeto
            if (id != producto.Id) return BadRequest("El ID del producto no coincide.");

            int miTenantId = ObtenerTenantIdSeguro();

            // 2. Buscamos el producto original en la base de datos
            var productoExistente = await _context.Productos.FindAsync(id);
            if (productoExistente == null) return NotFound("El producto no existe.");

            // 3. SEGURIDAD: Bloqueamos si el producto pertenece a otro negocio
            if (productoExistente.TenantId != miTenantId) return Forbid();

            // 4. Actualizamos solo los datos del producto (evitamos sobrescribir el TenantId)
            productoExistente.Nombre = producto.Nombre;
            productoExistente.Categoria = producto.Categoria;
            productoExistente.StockActual = producto.StockActual;
            productoExistente.PrecioVenta = producto.PrecioVenta;
            productoExistente.Costo = producto.Costo;
            productoExistente.StockMinimo = producto.StockMinimo;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent(); 
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            int miTenantId = ObtenerTenantIdSeguro();

            // 1. Buscamos el producto
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound("Producto no encontrado.");

            // 2. SEGURIDAD: Verificamos que sea el dueño antes de borrarlo
            if (producto.TenantId != miTenantId) return Forbid();

            // 3. Lo removemos de la base de datos
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}