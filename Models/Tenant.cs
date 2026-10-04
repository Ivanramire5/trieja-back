
namespace InventarioSaaS.Api.Models;

// 1. Manejo de Suscripción y Cliente
public class Tenant
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EstadoSuscripcion { get; set; } = "Inactivo"; // Activo, Vencido, Inactivo
    public DateTime FechaVencimiento { get; set; }
}