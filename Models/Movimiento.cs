
namespace InventarioSaaS.Api.Models;
public class Movimiento
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public string Tipo { get; set; } = string.Empty; // "Entrada" o "Salida"
    public int Cantidad { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Motivo { get; set; } = string.Empty; // Ej: "Venta", "Reposición"
}