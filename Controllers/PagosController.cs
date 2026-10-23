using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; 
using MercadoPago.Config;
using MercadoPago.Client.Preference;
using MercadoPago.Resource.Preference;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Triada.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PagosController : ControllerBase
    {
        private readonly IConfiguration _config;

        public PagosController(IConfiguration config)
        {
            _config = config;
            // Lee el token de forma segura desde los archivos de configuración
            MercadoPagoConfig.AccessToken = _config["MercadoPago:AccessToken"];
        }

        [HttpPost("crear-suscripcion")]
        public async Task<IActionResult> CrearSuscripcion([FromBody] SuscripcionRequest req)
        {
            try
            {
                var request = new PreferenceRequest
                {
                    Items = new List<PreferenceItemRequest>
                    {
                        new PreferenceItemRequest
                        {
                            Title = "Triada SaaS - Plan Mensual",
                            Quantity = 1,
                            CurrencyId = "ARS",
                            UnitPrice = 15000m
                        }
                    },
                    Payer = new PreferencePayerRequest
                    {
                        Email = req.Email
                    },
                    BackUrls = new PreferenceBackUrlsRequest
                    {
                        Success = "http://localhost:5173/dashboard",
                        Failure = "http://localhost:5173/",
                        Pending = "http://localhost:5173/dashboard"
                    }
                };

                var client = new PreferenceClient();
                Preference preference = await client.CreateAsync(request);

                return Ok(new { linkPago = preference.InitPoint });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { error = "Error al crear link de pago", detalle = ex.Message });
            }
        }
    }

    public class SuscripcionRequest
    {
        public string Email { get; set; }
    }
}