using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.FormFlow;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class RoomReservationCriteria : RoomBaseCriteria
    {
        public string Room { get; set; }

        public static IForm<RoomReservationCriteria> BuildForm()
        {
            return new FormBuilder<RoomReservationCriteria>()
                .Message("Let's book a conference room.")
                .AddRemainingFields()
                .Build();
        }

        public override string ToString()
        {
            return $"{this.Room} from {this.StartTime:h:mm tt} to {this.EndTime:h:mm tt}";
        }
    }
}