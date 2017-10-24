using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class RoomSearchCriteria : RoomBaseCriteria
    {
        public enum EquipmentOptions
        {
            Whiteboard = 1,
            Telephone,
            Display,
            None,
        }

        [Template(TemplateUsage.EnumSelectMany, "What equipment do you need? {||}", ChoiceStyle = ChoiceStyleOptions.PerLine)]
        public List<EquipmentOptions> Equipment;
        public int? NumberOfPeople;

        public static IForm<RoomSearchCriteria> BuildForm()
        {
            return new FormBuilder<RoomSearchCriteria>()
                .Message("Let's find you a conference room.")
                .AddRemainingFields()
                .Build();
        }

        public override string ToString()
        {
            var searchMsg = $"a room for {this.NumberOfPeople} people";
            if (this.Equipment.Any())
            {
                if (this.Equipment.Count == 1)
                {
                    searchMsg += $" with a {this.Equipment[0]}";
                }
                else if (this.Equipment.Count == 2)
                {
                    searchMsg += $" with a {this.Equipment[0]} and a {this.Equipment[1]}";
                }
                else
                {
                    searchMsg += " with " + string.Join(", ", this.Equipment.Select((i, ix) => (ix == this.Equipment.Count - 1 ? "and " : "") + $"a {i}"));
                }
            }
            searchMsg += $" from {this.StartTime:h:mm tt} to {this.EndTime:h:mm tt}";

            return searchMsg;
        }

        public static RoomSearchCriteria ParseCriteria(LuisResult result)
        {
            var numbers = result.Entities.Where(i => i.Type == "builtin.number")
                .Select(i => int.Parse((string)i.Resolution["value"])).ToArray();
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
            var start = timeRange.HasValue
                ? timeRange.Value.start
                : time.Length >= 2
                    ? time[0]
                    : time.Length == 1 && duration.HasValue
                        ? time[0]
                        : GetAssumedStartTime(DateTime.Now);
            while (start < DateTime.Now.AddMinutes(-15))
            {
                start = start.AddDays(1);
            }

            var end = timeRange.HasValue
                ? timeRange.Value.end
                : time.Length >= 2
                    ? time[1]
                    : duration.HasValue
                        ? start.Add(duration.Value)
                        : start.Add(TimeSpan.FromMinutes(30));
            while (end < DateTime.Now.AddMinutes(-15))
            {
                end = end.AddDays(1);
            }

            var criteria = new RoomSearchCriteria()
            {
                StartTime = start,
                EndTime = end,
                Equipment = equipment.Select(ParseEquipment).Where(i => i.HasValue).Select(i => i.Value).ToList(),
                NumberOfPeople = size,
                Office = RoomSearchCriteria.OfficeOptions.Chicago,
            };
            return criteria;
        }

        protected static RoomSearchCriteria.EquipmentOptions? ParseEquipment(string input)
        {
            if (Enum.TryParse(input, out RoomSearchCriteria.EquipmentOptions option))
                return option;
            switch (input.ToLowerInvariant())
            {
                case "tv":
                case "screen":
                case "projector":
                    return RoomSearchCriteria.EquipmentOptions.Display;
                case "telephone":
                case "phone":
                case "speakerphone":
                    return RoomSearchCriteria.EquipmentOptions.Telephone;
                case "whiteboard":
                    return RoomSearchCriteria.EquipmentOptions.Whiteboard;
            }
            return null;
        }
    }
}