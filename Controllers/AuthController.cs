using Microsoft.AspNetCore.Mvc;
using InventarioSaaS.Api.Data;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using InventarioSaaS.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace InventarioSaaS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _db;

        public AuthController(IConfiguration configuration, IEmailService emailService, ApplicationDbContext db)
        {
            _configuration = configuration;
            _emailService = emailService;
            _db = db;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        [EnableRateLimiting("LoginPolicy")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var tenantIdDelUsuario = "1"; 

            var claims = new[]
            {
                new Claim("TenantId", tenantIdDelUsuario),
                new Claim(ClaimTypes.Name, request.Email)
            };

            var keyStr = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Falta la clave JWT en appsettings");
            // Cambia Encoding.ASCII por Encoding.UTF8
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            // NUEVO: Convertimos el objeto token a texto plano (string)
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                // En desarrollo local (HTTP), a veces Secure=true bloquea la cookie. 
                // Para producción con HTTPS, debe ser true.
                Secure = false, 
                // Lax permite que la cookie viaje entre tus puertos locales de React y C#
                SameSite = SameSiteMode.Lax, 
                Expires = DateTime.UtcNow.AddHours(8)
            };
            
            // Usamos 'tokenString' en lugar de 'token'
            Response.Cookies.Append("jwt_token", tokenString, cookieOptions);

            return Ok(new { mensaje = "Inicio de sesión exitoso" });
        }
        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
        }

        [HttpPost("recuperar-password")]
        [EnableRateLimiting("LoginPolicy")] // Escudo activado
        public async Task<IActionResult> RecuperarPassword([FromBody] ForgotPasswordRequest request)
        {
            // 1. En una app real, aquí verificas si el correo existe en tu base de datos:
            // var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
            // if (usuario == null) return Ok(...); // Nunca revelamos si falló para evitar que adivinen correos.

            // 2. Generamos un código temporal único
            var tokenRecuperacion = Guid.NewGuid().ToString();

            // 3. Armamos el link hacia tu pantalla de React (que crearemos luego)
            var resetLink = $"http://localhost:5173/reset-password?token={tokenRecuperacion}&email={request.Email}";

            // 4. Enviamos el correo oficial
            string asunto = "Recuperación de Contraseña - Triada SaaS";
            string cuerpo = $@"
                <div style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2>Recuperación de Contraseña</h2>
                    <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta.</p>
                    <p>Haz clic en el siguiente botón para crear una nueva:</p>
                    <a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; display: inline-block;'>Restablecer Contraseña</a>
                    <p style='margin-top: 20px; font-size: 12px; color: #666;'>Si no solicitaste esto, ignora este correo. El enlace expirará pronto.</p>
                </div>";

            await _emailService.SendEmailAsync(request.Email, asunto, cuerpo);

            // Siempre devolvemos éxito para no confirmar a los hackers qué correos existen
            return Ok(new { mensaje = "Si el correo existe en nuestro sistema, recibirás un enlace de recuperación." });
        }
        public class ResetPasswordRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            public string NuevaPassword { get; set; } = string.Empty;
        }

        [HttpPost("restablecer-password")]
        [EnableRateLimiting("LoginPolicy")] // Escudo anti-bots activado
        public async Task<IActionResult> RestablecerPassword([FromBody] ResetPasswordRequest request)
        {
            // 1. Buscamos al negocio (Tenant) en la base de datos
            var tenant = await _db.Tenants.FirstOrDefaultAsync(u => u.Email == request.Email);

            // 2. Validamos que exista, que el token sea exactamente el mismo y que no haya caducado (1 hora)
            if (tenant == null || tenant.ResetToken != request.Token || tenant.ResetTokenExpires < DateTime.UtcNow)
            {
                return BadRequest(new { error = "El enlace de recuperación es inválido o ha expirado." });
            }

            // 3. Actualizamos la contraseña con la nueva que escribió el usuario
            tenant.Password = request.NuevaPassword; 

            // 4. Limpiamos el token de la base de datos para que ese enlace no se pueda reusar
            tenant.ResetToken = null;
            tenant.ResetTokenExpires = null;

            // 5. Guardamos los cambios
            await _db.SaveChangesAsync();

            return Ok(new { mensaje = "Contraseña actualizada correctamente. Ya puedes iniciar sesión." });
        }
    }
}