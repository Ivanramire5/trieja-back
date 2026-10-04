
namespace InventarioSaaS.Api.Models;
public class TransaccionCaja
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Tipo { get; set; } = string.Empty; // "Ingreso" o "Egreso"
    public decimal Monto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
}