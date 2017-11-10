using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Bot.Models;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class DateExtensions
    {
        public static DateTimeOffset InTimeZone(this DateTime value, TimeZoneInfo tz)
        {
            return new DateTimeOffset(value, tz.GetUtcOffset(value));
        }
    }
}
