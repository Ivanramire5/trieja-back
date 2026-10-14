using Microsoft.AspNetCore.Mvc;
using InventarioSaaS.Api.Data;
using InventarioSaaS.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventarioSaaS.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProveedoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int ObtenerTenantIdSeguro()
        {
            var tenantClaim = User.FindFirst("TenantId");
            if (tenantClaim == null) throw new UnauthorizedAccessException("Token inválido");
            return int.Parse(tenantClaim.Value);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProveedores()
        {
            int tenantIdReal = ObtenerTenantIdSeguro();
            var proveedores = await _context.Proveedores
                .Where(p => p.TenantId == tenantIdReal)
                .OrderBy(p => p.Empresa)
                .ToListAsync();
            return Ok(proveedores);
        }

        [HttpPost]
        public async Task<IActionResult> CrearProveedor(Proveedor proveedor)
        {
            proveedor.TenantId = ObtenerTenantIdSeguro(); // Forzamos el ID del token, ignoramos lo que envíe React
            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(ObtenerProveedores), new { id = proveedor.Id }, proveedor);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> EditarProveedor(int id, Proveedor proveedorActualizado)
        {
            int tenantIdReal = ObtenerTenantIdSeguro();
            
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            if (proveedor.TenantId != tenantIdReal) return Forbid(); // Evita que editen proveedores ajenos

            proveedor.Nombre = proveedorActualizado.Nombre;
            proveedor.Empresa = proveedorActualizado.Empresa;
            proveedor.Telefono = proveedorActualizado.Telefono;
            proveedor.Email = proveedorActualizado.Email;
            proveedor.Notas = proveedorActualizado.Notas;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarProveedor(int id)
        {
            int tenantIdReal = ObtenerTenantIdSeguro();
            
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            if (proveedor.TenantId != tenantIdReal) return Forbid();

            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}