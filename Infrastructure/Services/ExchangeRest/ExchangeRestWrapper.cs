using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Exchange.WebServices.Data;
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
            var fields = "ShowAs,Attendees,ChangeKey,Start,End,Importance,IsAllDay,OnlineMeetingUrl,Organizer,Sensitivity,Subject";
            return Get<Response<CalendarEntry[]>>($"v2.0/users/{roomAddress}/calendarView?startDateTime={startDate:s}&endDateTime={endDate:s}&$top=1000&$select={fields}");
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

        public class Attendee
        {
            public EmailAddress EmailAddress { get; set; }
            public AttendeeStatus Status { get; set; }
            public string Type { get; set; }
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
        }

        public Task Truncate(string meetingUniqueId)
        {
            throw new NotImplementedException();
        }
    }
}
