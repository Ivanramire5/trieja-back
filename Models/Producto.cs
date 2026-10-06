
namespace InventarioSaaS.Api.Models;
public class Producto
{
    public int Id { get; set; }
    public int TenantId { get; set; } // Identifica a qué empresa pertenece
    public Tenant? Tenant { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Costo { get; set; }
    public decimal PrecioVenta { get; set; }
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
}