
namespace InventarioSaaS.Api.Models;

// 1. Manejo de Suscripción y Cliente
public class Tenant
{
    public int Id { get; set; }
    public string NombreEmpresa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string EstadoSuscripcion { get; set; } = string.Empty; // Ejemplo: "Activo", "Inactivo", "Pendiente"
    public DateTime FechaVencimiento { get; set; }
    public string? ResetToken { get; set; } 
    public DateTime? ResetTokenExpires { get; set; }
}