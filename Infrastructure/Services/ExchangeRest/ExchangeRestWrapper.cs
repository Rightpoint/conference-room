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
        private readonly string _defaultUser;
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected override Uri BaseUri { get; } = new Uri("https://outlook.office.com/api/");

        public ExchangeRestWrapper(HttpClient client, HttpClient longCallClient, string defaultUser = "me") : base(client, longCallClient)
        {
            _defaultUser = defaultUser;
        }

        public Task<Response<CalendarEntry[]>> GetCalendarEvents(string roomAddress, DateTime startDate, DateTime endDate)
        {
            var fields = "ShowAs,Attendees,ChangeKey,Start,End,Importance,IsAllDay,OnlineMeetingUrl,Organizer,Sensitivity,Subject,Location";
            return Get<Response<CalendarEntry[]>>($"v2.0/users/{roomAddress}/calendarView?startDateTime={startDate:s}&endDateTime={endDate:s}&$top=1000&$select={fields}");
        }

        public Task<CalendarEntry> GetCalendarEvent(string roomAddress, string uniqueId)
        {
            return Get<CalendarEntry>($"v2.0/users/{roomAddress}/Events('{uniqueId}')");
        }

        public async Task Truncate(string roomAddress, CalendarEntry originalItem, DateTime targetEndDate)
        {
            var oldEnd = originalItem.End.ToOffset();
            var oldStart = originalItem.Start.ToOffset();
            var date = targetEndDate < oldStart ? oldStart : targetEndDate > oldEnd ? oldEnd : new DateTimeOffset(targetEndDate);
            await Patch($"v2.0/users/{roomAddress}/Events('{originalItem.Id}')", new StringContent(JObject.FromObject(new { End = new DateTimeReference() { DateTime = date.UtcDateTime.ToString("o"), TimeZone = "UTC" } }).ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task SendMessage(Message message, string sender)
        {
            if (_defaultUser == "me")
            {
                sender = _defaultUser;
            }
            await Post($"v2.0/{sender}/sendmail", new StringContent(JObject.FromObject(new { Message = message, SaveToSentItems = false}).ToString(Formatting.None), Encoding.UTF8, "application/json"));
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
            return await Post<SubscriptionResponse>($"beta/{_defaultUser}/subscriptions", new StringContent(obj.ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }

        public async Task GetNotifications(NotificationRequest request, Action<NotificationResponse> callback, CancellationToken cancellationToken)
        {
            await PostStreamResponse($"beta/{_defaultUser}/GetNotifications", new StringContent(JObject.FromObject(request).ToString(Formatting.None), Encoding.UTF8, "application/json"),
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
