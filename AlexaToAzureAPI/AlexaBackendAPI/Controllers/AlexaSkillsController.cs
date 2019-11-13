using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace AlexaBackendAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class AlexaSkillsController : Controller
    {
        private TelemetryClient _telemetry;
        private IConfiguration _configuration;
        public AlexaSkillsController(IConfiguration configuration, TelemetryClient telemetry)
        {
            _telemetry = telemetry;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Version()
        {
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            return Ok($"{assemblyName.Name} (v{assemblyName.Version.ToString()})");
        }
        [HttpPost]
        public async Task<SkillResponse> Post([FromBody]SkillRequest request)
        {            
            SkillResponse response = null;
            var props = new Dictionary<string, string>();

            if (request != null)
            {
                _telemetry.TrackEvent("Alexa skill call", GetTelemetryProperties(request));

                PlainTextOutputSpeech outputSpeech = new PlainTextOutputSpeech();
                var intent = (request.Request as IntentRequest)?.Intent;
                var name = User.Claims.FirstOrDefault(x => x.Type == "name")?.Value 
                    ?? User.Identity.Name;

                if (intent != null)
                {
                    switch (intent.Name.ToLowerInvariant())
                    {
                        case "quiensoy":
                            outputSpeech.Text = $"Esa es fácil, eres {name}";
                            break;

                        case "saludar":
                            string firstName = (request.Request as IntentRequest)?.Intent.Slots.FirstOrDefault(s => s.Key == "FirstName").Value.Value;
                            outputSpeech.Text = $"Hola {firstName}";
                            break;

                        case "miseventos":
                            var userId = User.Claims.FirstOrDefault(x => x.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                            var graphClient = new Graph.GraphClient(
                                _configuration["Graph:ClientId"],
                                _configuration["Graph:ClientSecret"],
                                _configuration["Graph:TenantId"]);

                            var myEvents = await graphClient.GetCalendarViewAsync(userId, 
                                DateTime.UtcNow.Date,
                                DateTime.UtcNow.Date.AddDays(1));
                            if (myEvents.Values.Count == 0)
                            {
                                outputSpeech.Text = "No tienes eventos en el calendario.";
                            }
                            else
                            {
                                outputSpeech.Text = $"Tienes {myEvents.Values.Count} eventos: ";
                                foreach (var e in myEvents.Values)
                                {
                                    outputSpeech.Text += $"{e.Subject} a las {e.Start.DateTime.ToShortTimeString()}";
                                }
                            }
                            break;

                        default:
                            outputSpeech.Text = $"Parece que al desarrollador de esta skill se le olvidó programar la intención {intent.Name}. Déjale tiempo a ver si baja de las nubes.";
                            break;
                    }
                }
                else
                {
                    outputSpeech.Text = $"No tengo muy claro lo que me quieres pedir {name}";
                }
                response = ResponseBuilder.Tell(outputSpeech);
            }

            return response;
        }

        private Dictionary<string, string> GetTelemetryProperties(SkillRequest request)
        {
            var prop = new Dictionary<string, string>();
            foreach(var claim in User.Claims)
            {
                prop.AddIfNotNull(claim.Type, claim.Value);
            }
            prop.AddIfNotNull("Intent", (request.Request as IntentRequest)?.Intent?.Name);
            return prop;
        }


    }
}
