using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// 1. Configuramos la llave secreta (En producción, esto va en un archivo seguro .env)
var key = Encoding.ASCII.GetBytes("EstaEsUnaClaveSuperSecretaYMuyLargaParaInventarioSaaS123!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


//app.UseHttpsRedirection();
app.UseCors("PermitirReact");
app.MapControllers();
app.UseAuthentication(); 
app.UseAuthorization();
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.Run();