using System;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria
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
    }
}