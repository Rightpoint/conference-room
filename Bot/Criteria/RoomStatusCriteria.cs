using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis.Models;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
{
    [Serializable]
    public class RoomStatusCriteria : RoomBaseCriteria
    {
        public string Room
        {
            get { return _room; }
            set
            {
                _room = value;
                if ((_room ?? "").ToLowerInvariant() == "away sis")
                    _room = "oasis";
            }
        }

        public string Building { get; set; }

        private string _room;
        
        public override string ToString()
        {
            var searchMsg = $"{this.Room}";
            if (!string.IsNullOrEmpty(this.Building))
            {
                searchMsg += $" in {this.Building}";
            }
            if (this.StartTime.HasValue)
            {
                if (this.EndTime.HasValue)
                {
                    searchMsg += $" from {this.StartTime.ToSimpleTime()} to {this.EndTime.ToSimpleTime()}";
                }
                else
                {
                    searchMsg += $" at {this.StartTime.ToSimpleTime()}";
                }
            }

            return searchMsg;
        }

        public static RoomStatusCriteria ParseCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            var room = result.Entities
                .Where(i => i.Type == "room")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            var building = result.Entities
                .Where(i => i.Type == "building")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            var criteria = new RoomStatusCriteria()
            {
                Room = room,
                Building = building,
            };
            criteria.LoadTimeCriteria(result, timezone);
            return criteria;
        }
    }
}