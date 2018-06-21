using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Controllers
{
    [Route("api/Authorize")]
    public class AuthorizeController : Controller
    {
        [HttpPost]
        public async Task<object> Post(AuthHelper.AuthorizeArgs args)
        {
            try
            {
                Trace.WriteLine($"Webhook was triggered!");

                // Initialize the azure bot
                using (BotService.Initialize())
                {
                    // BotBuilder insists on getting from the default config - this overrides it
                    Conversation.UpdateContainer(b =>
                    {
                        b.RegisterInstance(new MicrosoftAppCredentials(
                            Config.GetAppSetting("MicrosoftAppId"),
                            Config.GetAppSetting("MicrosoftAppPassword")
                        ));
                    });
                    var message = await AuthHelper.Process(Request.GetRequestUri(), args.state, args.code, args.error, args.error_description);
                    return Content(message, "text/html");
                }
            }
            catch (Exception ex)
            {
                MessagesController.TelemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}
