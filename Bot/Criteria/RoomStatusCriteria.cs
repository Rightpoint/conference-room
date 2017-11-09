using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis.Models;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
{
    [Serializable]
    public class RoomStatusCriteria : BaseCriteria
    {
        public string Room
        {
            get { return _room; }
            set
            {
                _room = value;
                if ((_room ?? "").ToLowerInvariant() == "away sis")
                    _room = "oasis";
            }
        }

        private string _room;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        
        public override string ToString()
        {
            var searchMsg = $"{this.Room}";
            if (this.StartTime.HasValue)
            {
                if (this.EndTime.HasValue)
                {
                    searchMsg += $" from {this.StartTime:h:mm tt} to {this.EndTime:h:mm tt}";
                }
                else
                {
                    searchMsg += $" at {this.StartTime:h:mm tt}";
                }
            }

            return searchMsg;
        }

        public static RoomStatusCriteria ParseCriteria(LuisResult result)
        {
            var room = result.Entities
                .Where(i => i.Type == "room")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            var timeRange = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.timerange")
                .SelectMany(i => (List<object>)i.Resolution["values"])
                .Select(i => ParseTimeRange((IDictionary<string, object>)i))
                .FirstOrDefault(i => i.HasValue);
            var time = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.time")
                .SelectMany(i => (List<object>)i.Resolution["values"])
                .Select(i => ParseTime((IDictionary<string, object>)i))
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToArray();
            var duration = result.Entities
                .Where(i => i.Type == "builtin.datetimeV2.duration")
                .SelectMany(i => (List<object>)i.Resolution["values"])
                .Select(i => ParseDuration((IDictionary<string, object>)i))
                .FirstOrDefault(i => i.HasValue);

            var start = timeRange.HasValue
                ? timeRange.Value.start
                : time.Length >= 2
                    ? time[0]
                    : time.Length == 1 && duration.HasValue
                        ? time[0]
                        : (DateTime?)null;
            while (start.HasValue && start.Value < DateTime.Now.AddMinutes(-15))
            {
                start = start.Value.AddDays(1);
            }

            var end = timeRange.HasValue
                ? timeRange.Value.end
                : time.Length >= 2
                    ? time[1]
                    : duration.HasValue && start.HasValue
                        ? start.Value.Add(duration.Value)
                        : (DateTime?)null;
            while (end.HasValue && end.Value < DateTime.Now.AddMinutes(-15))
            {
                end = end.Value.AddDays(1);
            }

            var criteria = new RoomStatusCriteria()
            {
                Room = room,
                StartTime = start,
                EndTime = end,
            };
            return criteria;
        }
    }
}