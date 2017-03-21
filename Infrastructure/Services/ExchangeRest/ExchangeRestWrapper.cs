using System;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestWrapper : RestWrapperBase
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override Uri BaseUri { get; } = new Uri("https://outlook.office.com/api/");

        public ExchangeRestWrapper(HttpClient client, HttpClient longCallClient) : base(client, longCallClient)
        {
        }

        public async Task Truncate(string roomAddress, CalendarEntry originalItem, DateTime targetEndDate)
        {
            var oldEnd = originalItem.End.ToOffset();
            var oldStart = originalItem.Start.ToOffset();
            var date = targetEndDate < oldStart ? oldStart : targetEndDate > oldEnd ? oldEnd : new DateTimeOffset(targetEndDate);
            await Patch($"v2.0/users/{roomAddress}/Events('{originalItem.Id}')", new StringContent(JObject.FromObject(new { End = date.ToString("o"), TimeZone = "UTC" }).ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task SendMessage(Message message)
        {
            await Post($"v2.0/me/sendmail", new StringContent(JObject.FromObject(new { Message = message, SaveToSentItems = false}).ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task<CalendarEntry> CreateEvent(string roomAddress, CalendarEntry calendarEntry)
        {
            return await Post<CalendarEntry>($"v2.0/users/{roomAddress}/events", new StringContent(JObject.FromObject(calendarEntry).ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task<SubscriptionResponse> CreateNotification(string roomAddress)
        {
            var obj = JObject.FromObject(new
            {
                Resource = new Uri(BaseUri, $"beta/users/{roomAddress}/events"),
                ChangeType = "Created,Deleted,Updated",
            });
            obj["@odata.type"] = "#Microsoft.OutlookServices.StreamingSubscription";
            return await Post<SubscriptionResponse>($"beta/me/subscriptions", new StringContent(obj.ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task GetNotifications(NotificationRequest request, Action<NotificationResponse> callback, CancellationToken cancellationToken)
        {
            await PostStreamResponse($"beta/Me/GetNotifications", new StringContent(JObject.FromObject(request).ToString(Formatting.None), Encoding.UTF8, "application/json"),
                obj =>
                {
                    var type = obj.Property("@odata.type")?.Value?.ToString();
                    if (type == "#Microsoft.OutlookServices.Notification")
                    {
                        callback(obj.ToObject<NotificationResponse>());
                    }
                    else
                    {
                        log.DebugFormat("Ignoring notification of type {0}", type);
                    }
                }, cancellationToken);
        }
    }
}
