using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class DateTimeExtensions
    {
        public static string ToSimpleTime(this DateTimeOffset? value)
        {
            if (null == value)
                return "";
            var msg = $"{value:h:mm tt}";
            var now = DateTimeOffset.Now;
            if (now.Date <= value.Value.Date)
            {
                if (now.Date.AddDays(1) == value.Value.Date)
                {
                    msg += " tomorrow";
                }
                else if (now.Date.AddDays(7) > value.Value.Date)
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

        public static string ToSimpleTime(this DateTime? value)
        {
            if (null == value)
                return "";
            var msg = $"{value:h:mm tt}";
            var now = DateTime.Now;
            if (now.Date <= value.Value.Date)
            {
                if (now.Date.AddDays(1) == value.Value.Date)
                {
                    msg += " tomorrow";
                }
                else if (now.Date.AddDays(7) > value.Value.Date)
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
