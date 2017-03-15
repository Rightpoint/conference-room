using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Exchange.WebServices.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestWrapper : RestWrapperBase
    {
        protected override Uri BaseUri { get; } = new Uri("https://outlook.office.com/api/");

        public ExchangeRestWrapper(HttpClient client) : base(client)
        {
        }

        public Task<Response<CalendarEntry[]>> GetCalendarEvents(string roomAddress, DateTime startDate, DateTime endDate)
        {
            var fields = "ShowAs,Attendees,ChangeKey,Start,End,Importance,IsAllDay,OnlineMeetingUrl,Organizer,Sensitivity,Subject,Location";
            return Get<Response<CalendarEntry[]>>($"v2.0/users/{roomAddress}/calendarView?startDateTime={startDate:s}&endDateTime={endDate:s}&$top=1000&$select={fields}");
        }

        public Task<Response<CalendarEntry>> GetCalendarEvent(string roomAddress, string uniqueId)
        {
            return Get<Response<CalendarEntry>>($"v2.0/users/{roomAddress}/Events('{uniqueId}')");
        }

        public class Response<T>
        {
            public T Value { get; set; }
        }

        public class EmailAddress
        {
            public string Address { get; set; }
            public string Name { get; set; }
        }

        public class AttendeeStatus
        {
            public string Response { get; set; }
            public DateTimeOffset Time { get; set; }
        }

        public class Attendee : Recipient
        {
            public AttendeeStatus Status { get; set; }
            public string Type { get; set; }
        }

        public class Recipient
        {
            public EmailAddress EmailAddress { get; set; }
        }

        public class DateTimeReference
        {
            public string DateTime { get; set; }
            public string TimeZone { get; set; }

            public DateTimeOffset ToOffset()
            {
                if (TimeZone != "UTC")
                {
                    throw new ArgumentException($"Invalid timezone: {TimeZone}");
                }
                return DateTimeOffset.Parse(DateTime);
            }
        }

        public enum Importance
        {
            Low,
            Normal,
            High,
        }
        public enum Sensitivity
        {
            Normal,
            Personal,
            Private,
            Confidential,
        }

        public enum ShowAs
        {
            Free,
            Busy,
        }

        public class CalendarEntry
        {
            public Attendee[] Attendees { get; set; }
            public string ChangeKey { get; set; }
            public DateTimeReference End { get; set; }
            public Importance Importance { get; set; }
            public bool IsAllDay { get; set; }
            public DateTimeReference Start { get; set; }
            public string OnlineMeetingUrl { get; set; }
            public Attendee Organizer { get; set; }
            public Sensitivity Sensitivity { get; set; }
            public string Subject { get; set; }
            public ShowAs ShowAs { get; set; }
            public string Id { get; set; }
            public string Location { get; set; }
            public BodyContent Body { get; set; }
        }

        public class BodyContent
        {
            public string ContentType { get; set; }
            public string Content { get; set; }
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

        public class Message
        {
            public string Subject { get; set; }
            public BodyContent Body { get; set; }
            public Recipient[] ToRecipients { get; set; }
            public Recipient From { get; set; }
            public Recipient[] ReplyTo { get; set; }
        }

        public async Task<CalendarEntry> CreateEvent(string roomAddress, CalendarEntry calendarEntry)
        {
            return await Post<CalendarEntry>($"v2.0/users/{roomAddress}/events", new StringContent(JObject.FromObject(calendarEntry).ToString(Formatting.None), Encoding.UTF8, "application/json"));
        }
    }
}
