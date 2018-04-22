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
        
        public override string ToString()
        {
            return $"{this.Room} from {this.StartTime.ToSimpleTime()} to {this.EndTime.ToSimpleTime()}";
        }

        public static RoomBookingCriteria ParseCriteria(LuisResult result, TimeZoneInfo timezone)
        {
            var room = result.Entities
                .Where(i => i.Type == "room")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            var criteria = new RoomBookingCriteria()
            {
                Room = room,
            };
            criteria.LoadTimeCriteria(result, timezone);
            return criteria;
        }
    }
}