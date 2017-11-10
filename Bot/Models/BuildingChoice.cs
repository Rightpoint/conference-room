using System;

namespace RightpointLabs.ConferenceRoom.Bot.Models
{
    [Serializable]
    public class BuildingChoice
    {
        public string BuildingId { get; set; }
        public string BuildingName { get; set; }
        public string TimezoneId { get; set; }
    }
}