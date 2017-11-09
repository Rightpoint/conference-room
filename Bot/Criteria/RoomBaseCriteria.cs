using System;

namespace RightpointLabs.ConferenceRoom.Bot.Criteria
{
    [Serializable]
    public class RoomBaseCriteria : BaseCriteria
    {
        public DateTime? StartTime;
        public DateTime? EndTime;
    }
}