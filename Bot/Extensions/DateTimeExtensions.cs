using System;

namespace RightpointLabs.ConferenceRoom.Bot.Extensions
{
    public static class DateTimeExtensions
    {
        public static string ToSimpleTime(this DateTimeOffset? value)
        {
            if (null == value)
                return "";
            return value.Value.ToSimpleTime();
        }

        public static string ToSimpleTime(this DateTimeOffset value)
        {
            var msg = $"{value:h:mm tt}";
            var now = DateTimeOffset.Now;
            if (now.Date < value.Date)
            {
                if (now.Date.AddDays(1) == value.Date)
                {
                    msg += " tomorrow";
                }
                else if (now.Date.AddDays(7) > value.Date)
                {
                    msg += $" {value:dddd}";
                }
                else
                {
                    msg += $" {value:MMMM d}";
                }
            }
            return msg;
        }
    }
}
