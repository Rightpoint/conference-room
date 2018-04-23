using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using RightpointLabs.BotLib;
using RightpointLabs.BotLib.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Extensions;
using Activity = Microsoft.Bot.Connector.Activity;

namespace RightpointLabs.ConferenceRoom.Bot.Controllers
{
    [Route("api/messages")]
    public class MessagesController : Controller
    {
        private static string appInsightsKey = TelemetryConfiguration.Active.InstrumentationKey =
            Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process) ??
            Environment.GetEnvironmentVariable("BotDevAppInsightsKey", EnvironmentVariableTarget.Process) ??
            Config.GetAppSetting("APPINSIGHTS_INSTRUMENTATIONKEY") ??
            Config.GetAppSetting("BotDevAppInsightsKey");

        public static TelemetryClient TelemetryClient { get; } = new TelemetryClient() {InstrumentationKey = appInsightsKey};

        [HttpOptions]
        public async Task<object> Options()
        {
            // allow bot framework to be pointed to us by responding to an OPTIONS call
            return Ok();
        }

        [HttpPost]
        public async Task<object> Post([FromBody]Activity activity)
        {
            try
            {
                // Initialize the azure bot
                using (BotService.Initialize())
                {
                    // Deserialize the incoming activity
                    //string jsonContent = await req.Content.ReadAsStringAsync();
                    //var activity = JsonConvert.DeserializeObject<Activity>(jsonContent);

                    // authenticate incoming request and add activity.ServiceUrl to MicrosoftAppCredentials.TrustedHostNames
                    // if request is authenticated
                    var authHeader = this.Request.Headers.GetCommaSeparatedValues(HeaderNames.Authorization);
                    var authParts = authHeader[0].Split(new[] {' '}, 2);
                    var identityToken = await BotService.Authenticator.TryAuthenticateAsync(authParts[0], authParts[1], CancellationToken.None);
                    if (null == identityToken || !identityToken.Authenticated)
                    {
                        this.Response.Headers.Add("WWW-Authenticate", $"Bearer realm=\"{Request.Host}\"");
                        return Unauthorized();
                    }
                    identityToken.ValidateServiceUrlClaim(new[] {activity});
                    MicrosoftAppCredentials.TrustServiceUrl(activity.ServiceUrl);

                    if (activity != null)
                    {
                        // one of these will have an interface and process it
                        switch (activity.GetActivityType())
                        {
                            case ActivityTypes.Message:
                                var text = activity.AsMessageActivity().Text;
                                Trace.WriteLine($"Processing message: '{text}' from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
                                if (text.Contains("</at>") && activity.ChannelId == "msteams")
                                {
                                    // ignore the mention of us in the reply
                                    activity.AsMessageActivity().Text = new Regex("<at>.*</at>").Replace(text, "").Trim();
                                    Trace.WriteLine($"Revised: processing message: '{activity.AsMessageActivity().Text}' from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
                                }
                                await Conversation.SendAsync(activity, () => new ExceptionHandlerDialog<object>(new BotDialog(Request.GetRequestUri()), true));
                                break;
                            case ActivityTypes.ConversationUpdate:
                                var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                                IConversationUpdateActivity update = activity;
                                if (update.MembersAdded?.Any() ?? false)
                                {
                                    var reply = activity.CreateReply();
                                    var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                                    foreach (var newMember in newMembers)
                                    {
                                        reply.Text = "Welcome";
                                        if (!string.IsNullOrEmpty(newMember.Name))
                                        {
                                            reply.Text += $" {newMember.Name}";
                                        }
                                        reply.Text += ", this is a bot from Rightpoint Labs Beta - say 'info' for more.";
                                        await client.Conversations.ReplyToActivityAsync(reply);
                                    }
                                }
                                break;
                            case ActivityTypes.ContactRelationUpdate:
                            case ActivityTypes.Typing:
                            case ActivityTypes.DeleteUserData:
                            case ActivityTypes.Ping:
                            default:
                                Trace.WriteLine($"Unknown activity type ignored: {activity.GetActivityType()}");
                                break;
                        }
                    }
                    return Accepted();
                }
            }
            catch(Exception ex)
            {
                TelemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}
