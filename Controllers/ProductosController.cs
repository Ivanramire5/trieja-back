using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;
using InventarioSaaS.Api.Models;

namespace InventarioSaaS.Api.Controllers
{
    // Esto define la ruta base de este controlador. 
    // [controller] se reemplaza automáticamente por "Productos", por lo que la ruta será: /api/productos
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

        // GET: api/productos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Producto>>> GetProductos()
        {
            // Aquí usamos Entity Framework para ir a PostgreSQL y traer todos los productos.
            // (Más adelante, como es un SaaS, aquí filtraremos por el TenantId del cliente logueado)
            var productos = await _context.Productos.ToListAsync();
            
            return Ok(productos);
        }

        // POST: api/productos
        [HttpPost]
        public async Task<ActionResult<Producto>> CrearProducto(Producto producto)
        {
            // Agregamos el producto recibido desde React a la base de datos
            _context.Productos.Add(producto);
            await _context.SaveChangesAsync();

            // Retornamos un estado 201 Created y el producto recién creado (con su nuevo ID)
            return CreatedAtAction(nameof(GetProductos), new { id = producto.Id }, producto);
        }
        
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarProducto(int id, Producto producto)
        {
            // Verificamos que el ID de la URL coincida con el ID del objeto enviado
            if (id != producto.Id)
            {
                return BadRequest("El ID del producto no coincide.");
            }

            // Le decimos a Entity Framework que este producto ha sido modificado
            _context.Entry(producto).State = EntityState.Modified;

            try
            {
                // Guardamos los cambios en PostgreSQL
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Productos.Any(e => e.Id == id))
                {
                    return NotFound("El producto no existe.");
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // Retorna 204 No Content (Éxito sin devolver datos)
        }

        // DELETE: api/productos/{id} (Para Eliminar Producto)
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProducto(int id)
        {
            // Buscamos el producto en la base de datos
            var producto = await _context.Productos.FindAsync(id);
            
            if (producto == null)
            {
                return NotFound("Producto no encontrado.");
            }

            // Lo removemos del contexto y guardamos
            _context.Productos.Remove(producto);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}