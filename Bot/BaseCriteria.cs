using System;
using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class BaseCriteria
    {
        protected static DateTime GetAssumedStartTime(DateTime time)
        {
            var last15 = new DateTime(time.Year, time.Month, time.Day, time.Hour, (time.Minute / 15) * 15, 0, time.Kind);
            if (time.Minute % 15 > 10)
            {
                // round up
                return last15.Add(TimeSpan.FromMinutes(15));
            }
            // round down
            return last15;
        }

        protected static (DateTime start, DateTime end)? ParseTimeRange(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "timerange":
                    var start = DateTime.Parse((string)values["start"]);
                    var end = DateTime.Parse((string)values["end"]);
                    return (start, end);
                default:
                    return null;
            }
        }

        protected static DateTime? ParseTime(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "time":
                    return DateTime.Parse((string)values["value"]);
                default:
                    return null;
            }
        }

        protected static TimeSpan? ParseDuration(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "duration":
                    return TimeSpan.FromSeconds(int.Parse((string)values["value"]));
                default:
                    return null;
            }
        }
    }
}