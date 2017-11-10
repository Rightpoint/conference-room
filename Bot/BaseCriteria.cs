using System;
using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class BaseCriteria
    {
        protected static DateTimeOffset GetAssumedStartTime(DateTimeOffset time)
        {
            var last15 = new DateTimeOffset(time.Year, time.Month, time.Day, time.Hour, (time.Minute / 15) * 15, 0, time.Offset);
            if (time.Minute % 15 > 10)
            {
                // round up
                return last15.Add(TimeSpan.FromMinutes(15));
            }
            // round down
            return last15;
        }

        protected static (DateTimeOffset start, DateTimeOffset end)? ParseTimeRange(IDictionary<string, object> values, TimeZoneInfo timezone)
        {
            switch ((string)values["type"])
            {
                case "timerange":
                    var start = DateTime.Parse((string)values["start"]).InTimeZone(timezone);
                    var end = DateTime.Parse((string)values["end"]).InTimeZone(timezone);
                    return (start, end);
                default:
                    return null;
            }
        }

        protected static DateTimeOffset? ParseTime(IDictionary<string, object> values, TimeZoneInfo timezone)
        {
            switch ((string)values["type"])
            {
                case "time":
                case "datetime":
                    var value = DateTime.Parse((string)values["value"]);
                    if (values.TryGetValue("timex", out object timex))
                    {
                        if (timex is DateTime utcTime)
                        {
                            return TimeZoneInfo.ConvertTime(utcTime.InTimeZone(TimeZoneInfo.Utc), timezone);
                        }
                        else if(timex is string timexStr && timexStr == "PRESENT_REF")
                        {
                            return TimeZoneInfo.ConvertTime(value.InTimeZone(TimeZoneInfo.Utc), timezone);
                        }
                    }
                    return value.InTimeZone(timezone);
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