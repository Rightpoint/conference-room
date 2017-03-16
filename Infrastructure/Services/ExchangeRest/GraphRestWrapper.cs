using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class GraphRestWrapper : RestWrapperBase
    {
        protected override Uri BaseUri { get; } = new Uri("https://graph.microsoft.com/");

        public GraphRestWrapper(HttpClient client) : base(client)
        {
        }

        public async Task<string> GetUserDisplayName(string roomAddress)
        {
            try
            {
                return (await Get<UserResult>($"v1.0/users/{roomAddress}"))?.DisplayName;
            }
            catch (HttpRequestException ex)
            {
                if (!ex.Message.Contains("404 (Not Found)"))
                    throw;
                return (await Get<Response<UserResult[]>>($"v1.0/users?$filter=mail%20eq%20%27{roomAddress}%27"))?.Value?.SingleOrDefault()?.DisplayName;
            }
        }

        public Task<Response<CalendarEntry[]>> GetCalendarEvents(string roomAddress, DateTime startDate, DateTime endDate)
        {
            var fields = "ShowAs,Attendees,ChangeKey,Start,End,Importance,IsAllDay,OnlineMeetingUrl,Organizer,Sensitivity,Subject,Location";
            return Get<Response<CalendarEntry[]>>($"v1.0/users/{roomAddress}/calendarView?startDateTime={startDate:s}&endDateTime={endDate:s}&$top=1000&$select={fields}");
        }

        public Task<Response<CalendarEntry>> GetCalendarEvent(string roomAddress, string uniqueId)
        {
            return Get<Response<CalendarEntry>>($"v1.0/users/{roomAddress}/Events('{uniqueId}')");
        }

    }
}
