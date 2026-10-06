using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        // Pon los puertos exactos donde corre tu React en desarrollo
        policy.WithOrigins("http://localhost:5173", "http://localhost:5168") 
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
// --- CONEXIÓN A POSTGRESQL ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

//app.UseHttpsRedirection();
app.UseCors("PermitirReact");
app.MapControllers();
app.MapGet("/api/crear-tenant", async (InventarioSaaS.Api.Data.ApplicationDbContext db) =>
{
    var nuevoTenant = new InventarioSaaS.Api.Models.Tenant 
    { 
        NombreEmpresa = "Mi Primer Negocio", // Usamos el nombre correcto de tu propiedad
        Email = "contacto@miprimernegocio.com", // Llenamos los demás campos por si acaso
        EstadoSuscripcion = "Activo",
        FechaVencimiento = DateTime.UtcNow.AddYears(1)
    };
    
    db.Tenants.Add(nuevoTenant);
    await db.SaveChangesAsync();
    
    return Results.Ok($"¡Negocio creado exitosamente con el ID: {nuevoTenant.Id}!");
});
app.Run();