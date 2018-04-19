using System;
using System.Configuration;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class Authorize
    {
        [FunctionName("Authorize")]
        public static async Task<object> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST")]HttpRequestMessage req, TraceWriter log)
        {
            Messages.TelemetryClient.Context.Operation.Id = Guid.NewGuid().ToString();
            Messages.TelemetryClient.Context.Operation.Name = "cs-http";

            try
            {
                log.Info($"Webhook was triggered!");

                // Initialize the azure bot
                using (new AzureFunctionsResolveAssembly())
                using (BotService.Initialize())
                {
                    // Deserialize the incoming activity
                    var formData = await req.Content.ReadAsFormDataAsync();

                    return await AuthHelper.Process(req.RequestUri, formData["state"], formData["code"], formData["error"], formData["error_description"]);
                }
            }
            catch (Exception ex)
            {
                Messages.TelemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}
