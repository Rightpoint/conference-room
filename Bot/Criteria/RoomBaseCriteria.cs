using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Luis.Models;
using RightpointLabs.BotLib.Extensions;
using RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
{
    [Serializable]
    public class RoomBaseCriteria : BaseCriteria
    {
        public DateTimeOffset? StartTime;
        public DateTimeOffset? EndTime;

        public void LoadTimeCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            var timeRange = result.ParseTimeRange(timezone);
            var time = result.ParseTime(timezone);
            var duration = result.ParseDuration();

            var start = timeRange.HasValue
                ? timeRange.Value.start
                : time.Length >= 1
                    ? time[0]
                    : time.Length == 1 && duration.HasValue
                        ? time[0]
                        : (DateTimeOffset?)null;
            if (start.HasValue && start >= DateTime.Now.AddSeconds(-10) && start <= DateTime.Now.AddSeconds(10))
            {
                // user said "now".. let's adjust a bit
                start = GetAssumedStartTime(start.Value);
            }
            while (start.HasValue && start < DateTime.Now.AddMinutes(-15))
            {
                start = start.Value.AddDays(1);
            }

            var end = timeRange.HasValue
                ? timeRange.Value.end
                : time.Length >= 2
                    ? time[1]
                    : duration.HasValue && start.HasValue
                        ? start.Value.Add(duration.Value)
                        : (DateTimeOffset?)null;
            while (end.HasValue && end < DateTime.Now.AddMinutes(-15))
            {
                end = end.Value.AddDays(1);
            }

            this.StartTime = start;
            this.EndTime = end;
        }

        public void LoadEndTimeCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            var time = result.ParseTime(timezone);
            var duration = result.ParseDuration();

            var end = time.Length >= 1
                ? time[0]
                : duration.HasValue && this.StartTime.HasValue
                    ? this.StartTime.Value.Add(duration.Value)
                    : (DateTimeOffset?)null;
            while (end.HasValue && end < DateTime.Now.AddMinutes(-15))
            {
                end = end.Value.AddDays(1);
            }

            this.EndTime = end;
        }
    }
}