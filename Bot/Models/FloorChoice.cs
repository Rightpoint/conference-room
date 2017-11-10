using System;

namespace RightpointLabs.ConferenceRoom.Bot.Models
{
    [Serializable]
    public class FloorChoice
    {
        public string FloorId { get; set; }
        public string Floor { get; set; }
        public string FloorName { get; set; }
    }
}