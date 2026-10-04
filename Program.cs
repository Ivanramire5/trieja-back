using Microsoft.EntityFrameworkCore;
using InventarioSaaS.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// --- CONEXIÓN A POSTGRESQL ---
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();
app.Run();