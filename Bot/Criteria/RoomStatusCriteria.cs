using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Builder.Luis.Models;

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

        private string _room;
        
        public override string ToString()
        {
            var searchMsg = $"{this.Room}";
            if (this.StartTime.HasValue)
            {
                if (this.EndTime.HasValue)
                {
                    searchMsg += $" from {this.StartTime:h:mm tt} to {this.EndTime:h:mm tt}";
                }
                else
                {
                    searchMsg += $" at {this.StartTime:h:mm tt}";
                }
            }

            return searchMsg;
        }

        public static RoomStatusCriteria ParseCriteria(LuisResult result)
        {
            var room = result.Entities
                .Where(i => i.Type == "room")
                .Select(i => i.Entity ?? (string)i.Resolution["value"])
                .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            var criteria = new RoomStatusCriteria()
            {
                Room = room,
            };
            criteria.LoadTimeCriteria(result);
            return criteria;
        }
    }
}