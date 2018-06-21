using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;
using RightpointLabs.ConferenceRoom.Bot.Criteria;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
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
        public string Building;
        
        public override string ToString()
        {
            var searchMsg = $"a room for {this.NumberOfPeople} people";
            if (!string.IsNullOrEmpty(this.Building))
            {
                searchMsg += $" in {this.Building}";
            }
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
            searchMsg += $" from {this.StartTime.ToSimpleTime()} to {this.EndTime.ToSimpleTime()}";

            return searchMsg;
        }

        public static RoomSearchCriteria ParseCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            // when looking for a number, don't count numbers that are part of another recommendation (ie. "40 minutes" is a duration, not a number of people)
            bool isSubstring(EntityRecommendation entity)
            {
                return result.Entities.Any(r => r != entity && r.StartIndex <= entity.StartIndex && entity.EndIndex <= r.EndIndex);
            }
            var numbers = result.Entities.Where(i => i.Type == "builtin.number").Where(i => !isSubstring(i))
                .Select(i => int.Parse((string)i.Resolution["value"])).ToArray();
            var equipment = result.Entities.Where(i => i.Type == "equipment").Select(i => i.Entity).ToArray();
            var size = numbers.Cast<int?>().FirstOrDefault();
            var building = result.Entities.Where(i => i.Type == "building").Select(i => i.Entity).FirstOrDefault();

            var criteria = new RoomSearchCriteria()
            {
                Equipment = equipment.Select(ParseOneEquipment).Where(i => i.HasValue).Select(i => i.Value).ToList(),
                NumberOfPeople = size,
                Building = building,
            };

            criteria.LoadTimeCriteria(result, timezone);
            return criteria;
        }

        private static readonly Regex _cleanup = new Regex("[^A-Za-z ]*", RegexOptions.Compiled);

        public void ParseEquipment(string input)
        {
            input = _cleanup.Replace(input ?? "", "");

            this.Equipment = input.Split(new[] {" and ", ","}, StringSplitOptions.RemoveEmptyEntries).Select(ParseOneEquipment).Where(i => i != null).Select(i => i.Value).ToList();
            if (!this.Equipment.Any())
            {
                this.Equipment = new List<EquipmentOptions>() {EquipmentOptions.None};
            }
        }

        private static EquipmentOptions? ParseOneEquipment(string input)
        {
            input = _cleanup.Replace(input ?? "", "");
            input = input.ToLowerInvariant().Trim();
            if (Enum.TryParse(input, out RoomSearchCriteria.EquipmentOptions option))
                return option;
            switch (input.ToLowerInvariant())
            {
                case "tv":
                case "screen":
                case "projector":
                case "monitor":
                    return RoomSearchCriteria.EquipmentOptions.Display;
                case "telephone":
                case "phone":
                case "speaker phone":
                case "speakerphone":
                    return RoomSearchCriteria.EquipmentOptions.Telephone;
                case "white board":
                case "whiteboard":
                    return RoomSearchCriteria.EquipmentOptions.Whiteboard;
            }
            return null;
        }
    }
}