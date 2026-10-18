using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// --- 1. REGISTRO DE SERVICIOS INDEPENDIENTES ---
// Aquí registramos el servicio de correos en la raíz, fuera de cualquier otra configuración
builder.Services.AddScoped<InventarioSaaS.Api.Services.IEmailService, InventarioSaaS.Api.Services.EmailService>();

// --- 2. CONFIGURACIÓN DE CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirReact", policy =>
    {
        policy.WithOrigins("http://localhost:5173") 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// --- 3. CONFIGURACIÓN DE RATE LIMITER ---
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginPolicy", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// --- 4. CONEXIÓN A BASE DE DATOS ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// --- 5. SEGURIDAD Y JWT ---
var keyStr = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta la clave JWT en appsettings");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true, 
            // Usamos UTF8 directamente
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Cookies["jwt_token"];
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("=== ERROR DE AUTENTICACIÓN ===");
                Console.WriteLine(context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- 6. MIDDLEWARES (El orden es estricto) ---
app.UseRouting();

// Activa el escudo de Rate Limiting
app.UseRateLimiter();

// 1. CORS
app.UseCors("PermitirReact");

// 2. Autenticación (Leer Cookie)
app.UseAuthentication(); 

// 3. Autorización (Validar permisos)
app.UseAuthorization();

// 4. Controladores
app.MapControllers();

// --- 7. ENDPOINTS MÍNIMOS DE PRUEBA ---
app.MapGet("/api/crear-tenant", async (InventarioSaaS.Api.Data.ApplicationDbContext db) =>
{
    var nuevoTenant = new InventarioSaaS.Api.Models.Tenant 
    { 
        NombreEmpresa = "Mi Primer Negocio", 
        Email = "contacto@miprimernegocio.com", 
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