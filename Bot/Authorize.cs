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
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            // Initialize the azure bot
            using (new AzureFunctionsResolveAssembly())
            using (BotService.Initialize())
            {
                // Deserialize the incoming activity
                var formData = await req.Content.ReadAsFormDataAsync();

                var cookie = SecureUrlToken.Decode<ResumptionCookie>(formData["state"]);
                if (!string.IsNullOrEmpty(formData["error"]))
                {
                    await Conversation.ResumeAsync(cookie, new AuthenticationResultModel(cookie.GetMessage()) { Error = formData["error"], ErrorDescription = formData["error_description"] });
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><head><script type='text/javascript'>window.close();</script></head><body>An error occurred during authentication.  You can close this browser window</body></html>", Encoding.UTF8, "text/html")
                    };
                }

                // Get access token
                var authContext = new AuthenticationContext(ConfigurationManager.AppSettings["Authority"]);
                var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
                    formData["code"],
                    new Uri(req.RequestUri.GetLeftPart(UriPartial.Path)),
                    new ClientCredential(
                        ConfigurationManager.AppSettings["ClientId"],
                        ConfigurationManager.AppSettings["ClientSecret"]));

                var upn = authResult?.UserInfo?.DisplayableId;

                var result = new AuthenticationResultModel(cookie.GetMessage())
                {
                    AccessToken = authResult.IdToken
                };

                if (upn == cookie.GetMessage().From.Id)
                {
                    await Conversation.ResumeAsync(cookie, result);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("<html><head><script type='text/javascript'>window.close();</script></head><body>You can close this browser window</body></html>", Encoding.UTF8, "text/html")
                    };
                }
                else
                {
                    var rnd = new Random();
                    result.SecurityKey = string.Join("", Enumerable.Range(0, 6).Select(i => rnd.Next(10).ToString()));
                    await Conversation.ResumeAsync(cookie, result);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"<html><head></head><body><!--We can't auto-auth you because {upn} != {cookie.GetMessage().From.Id}. -->Please copy and paste this key into the conversation with the bot: {result.SecurityKey}.</body></html>", Encoding.UTF8, "text/html")
                    };
                }
            }

        }
    }
}
