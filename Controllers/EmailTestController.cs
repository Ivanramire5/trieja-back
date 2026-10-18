using Microsoft.AspNetCore.Mvc;
using InventarioSaaS.Api.Services;

namespace InventarioSaaS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailTestController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailTestController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("enviar-prueba")]
        public async Task<IActionResult> EnviarPrueba([FromQuery] string correoDestino)
        {
            try
            {
                string asunto = "¡Prueba de Integración Exitosa - Triada SaaS!";
                string htmlCuerpo = @"
                    <h2>¡Configuración perfecta!</h2>
                    <p>Si estás leyendo esto, tu backend en C# se ha comunicado exitosamente con el servidor SMTP.</p>
                    <p>El siguiente paso será usar esto para recuperar contraseñas y verificar usuarios.</p>";
                
                await _emailService.SendEmailAsync(correoDestino, asunto, htmlCuerpo);
                
                return Ok(new { mensaje = "Correo enviado con éxito. Revisa tu bandeja." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Falló el envío", detalle = ex.Message });
            }
        }
    }
}