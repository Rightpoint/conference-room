using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
{
    [Serializable]
    public class RoomBookingCriteria : RoomBaseCriteria
    {
        private string _room;

        public RoomBookingCriteria()
        {
        }

        public RoomBookingCriteria(RoomBaseCriteria baseCriteria)
        {
            this.StartTime = baseCriteria.StartTime;
            this.EndTime = baseCriteria.EndTime;
        }

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

        public override string ToString()
        {
            var desc = this.Room;
            if (!string.IsNullOrEmpty(this.Building))
            {
                desc += $" {this.Building}";
            }
            desc += $" from {this.StartTime.ToSimpleTime()} to {this.EndTime.ToSimpleTime()}";
            return desc;
        }

        public static RoomBookingCriteria ParseCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            var room = result.Entities
                .Where(i => i.Type == "room")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            var building = result.Entities
                .Where(i => i.Type == "building")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            var criteria = new RoomBookingCriteria()
            {
                Room = room,
                Building = building,
            };
            criteria.LoadTimeCriteria(result, timezone);
            return criteria;
        }

    }
}