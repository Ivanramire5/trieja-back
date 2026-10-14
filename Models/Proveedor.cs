namespace InventarioSaaS.Api.Models
{
    public class Proveedor
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Empresa { get; set; } = string.Empty; // Ej: Coca-Cola, Distribuidora XYZ
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty; // Para anotar días de visita o condiciones
        
        // El candado de seguridad: a qué negocio pertenece este proveedor
        public int TenantId { get; set; }
    }
}