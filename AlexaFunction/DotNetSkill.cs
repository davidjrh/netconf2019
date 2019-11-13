using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Alexa.NET.Request;
using Alexa.NET.Response;
using Alexa.NET.Request.Type;
using Alexa.NET;

namespace AlexaFunction
{
    public static class DotNetSkill
    {
        [FunctionName("dot-net-skill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string json = await req.ReadAsStringAsync();
            var skillRequest = JsonConvert.DeserializeObject<SkillRequest>(json);
            return ProcessRequest(skillRequest);
        }

        private static IActionResult ProcessRequest(SkillRequest skillRequest)
        {
            var requestType = skillRequest.GetRequestType();
            SkillResponse response = ResponseBuilder.Tell("¡Hasta luego!");
            response.Response.ShouldEndSession = true;

            if (requestType == typeof(LaunchRequest))
            {
                response = ResponseBuilder.Tell("¡Bienvenidos a la dot net Conf!");
                response.Response.ShouldEndSession = false;
            }
            else if (requestType == typeof(IntentRequest))
            {
                var intentRequest = skillRequest.Request as IntentRequest;
                if (intentRequest.Intent.Name == "Hello" || intentRequest.Intent.Name == "Hola")
                {
                    response = ResponseBuilder.Tell("¡Hola desde la Skill de Dot Net!");
                    response.Response.ShouldEndSession = false;
                }
            }
            return new OkObjectResult(response);
        }
    }
}
