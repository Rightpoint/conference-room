using System;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class Messages
    {
        private static string appInsightsKey = TelemetryConfiguration.Active.InstrumentationKey =
            Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", EnvironmentVariableTarget.Process) ??
            Environment.GetEnvironmentVariable("BotDevAppInsightsKey", EnvironmentVariableTarget.Process);

        public static TraceWriter CurrentLog { get; private set; }

        public static TelemetryClient TelemetryClient { get; } = new TelemetryClient() {InstrumentationKey = appInsightsKey};

        [FunctionName("messages")]
        public static async Task<object> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST")] HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            CurrentLog = log; // not sure if we get a private runtime for each request or not....
            log.Info($"Webhook was triggered!");

            TelemetryClient.Context.Operation.Id = context.InvocationId.ToString();
            TelemetryClient.Context.Operation.Name = "cs-http";

            // force assembly to load early with the right version number
            var x = typeof(Conversation);

            try
            {
                // Initialize the azure bot
                using (new AzureFunctionsResolveAssembly())
                using (BotService.Initialize())
                {
                    return await RunBody(req, context, log);
                }
            }
            catch(Exception ex)
            {
                TelemetryClient.TrackException(ex);
                throw;
            }
        }

        // this is split into a second method so that the assemblies it references (like Microsoft.Bot.Connector) don't load until *after* the AzureFunctionsResolveAssembly hook (above) is in place.
        public static async Task<object> RunBody(HttpRequestMessage req, ExecutionContext context, TraceWriter log)
        {
            // Deserialize the incoming activity
            string jsonContent = await req.Content.ReadAsStringAsync();
            var activity = JsonConvert.DeserializeObject<Activity>(jsonContent);

            // authenticate incoming request and add activity.ServiceUrl to MicrosoftAppCredentials.TrustedHostNames
            // if request is authenticated
            if (!await BotService.Authenticator.TryAuthenticateAsync(req, new[] {activity}, CancellationToken.None))
            {
                return BotAuthenticator.GenerateUnauthorizedResponse(req);
            }

            if (activity != null)
            {
                // one of these will have an interface and process it
                switch (activity.GetActivityType())
                {
                    case ActivityTypes.Message:
                        var text = activity.AsMessageActivity().Text;
                        log.Info(
                            $"Processing message: '{text}' from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
                        if (text.Contains("</at>") && activity.ChannelId == "msteams")
                        {
                            // ignore the mention of us in the reply
                            activity.AsMessageActivity().Text = new Regex("<at>.*</at>").Replace(text, "").Trim();
                            log.Info(
                                $"Revised: processing message: '{activity.AsMessageActivity().Text}' from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
                        }
                        await Conversation.SendAsync(activity,
                            () => new ExceptionHandlerDialog<object>(new BotDialog(req.RequestUri), true));
                        break;
                    case ActivityTypes.ConversationUpdate:
                        var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                        IConversationUpdateActivity update = activity;
                        if (update.MembersAdded.Any())
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
                        log.Error($"Unknown activity type ignored: {activity.GetActivityType()}");
                        break;
                }
            }
            return req.CreateResponse(HttpStatusCode.Accepted);
        }
    }
}
