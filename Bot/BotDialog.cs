using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class BotDialog : LuisDialog<object>
    {
        public BotDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
        {
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I don't know what you meant.  You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        [LuisIntent("findRoom")]
        public async Task FindRoom(IDialogContext context, LuisResult result)
        {
            var numbers = result.Entities.Where(i => i.Type == "builtin.number").Select(i => int.Parse((string)i.Resolution["value"])).ToArray();
            var equipment = result.Entities.Where(i => i.Type == "equipment").Select(i => i.Entity).ToArray();
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

            var size = numbers.Cast<int?>().FirstOrDefault() ?? 6;
            var start = timeRange.HasValue ? 
                timeRange.Value.start : 
                time.Length >= 2 ?
                    time[0] :
                    time.Length == 1 && duration.HasValue ?
                        time[0] :
                        GetAssumedStartTime(DateTime.Now);
            var end = timeRange.HasValue
                ? timeRange.Value.end
                : time.Length >= 2
                    ? time[1]
                    : duration.HasValue
                        ? start.Add(duration.Value)
                        : start.Add(TimeSpan.FromMinutes(30));

            // searching...
            var searchMsg = $"Searching for a room for {size} people";
            if (equipment.Any())
            {
                if (equipment.Length == 1)
                {
                    searchMsg += $" with a {equipment[0]}";
                }
                else if (equipment.Length == 2)
                {
                    searchMsg += $" with a {equipment[0]} and a {equipment[1]}";
                }
                else
                {
                    searchMsg += " with " + string.Join(", ", equipment.Select((i, ix) => (ix == equipment.Length - 1 ? "and " : "") + $"a {i}"));
                }
            }
            searchMsg += $" from {start:h:mm tt} to {end:h:mm tt}";

            var msg = context.MakeMessage();
            msg.Text = searchMsg;
            msg.Speak = searchMsg;
            msg.InputHint = InputHints.IgnoringInput;
            await context.PostAsync(msg);

            await Task.Delay(TimeSpan.FromSeconds(1));

            var msg2 = context.MakeMessage();
            msg2.Text = "Sorry, search not implemented yet";
            msg2.Speak = "Sorry, search not implemented yet";
            msg2.InputHint = InputHints.AcceptingInput;
            await context.PostAsync(msg2);

            context.Wait(MessageReceived);
        }

        private DateTime GetAssumedStartTime(DateTime time)
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

        private (DateTime start, DateTime end)? ParseTimeRange(IDictionary<string, object> values)
        {
            switch ((string) values["type"])
            {
                case "timerange":
                    var start = DateTime.Parse((string)values["start"]);
                    var end = DateTime.Parse((string)values["end"]);
                    return (start, end);
                default:
                    return null;
            }
        }

        private DateTime? ParseTime(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "time":
                    return DateTime.Parse((string)values["value"]);
                default:
                    return null;
            }
        }

        private TimeSpan? ParseDuration(IDictionary<string, object> values)
        {
            switch ((string)values["type"])
            {
                case "duration":
                    return TimeSpan.FromSeconds(int.Parse((string)values["value"]));
                default:
                    return null;
            }
        }

        [LuisIntent("bookRoom")]
        public async Task BookRoom(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached the bookRoom intent. You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }
    }
}

