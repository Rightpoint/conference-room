using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class RoomInfo
    {
        public DateTime CurrentTime { get; set; }
        public string DisplayName { get; set; }
        public SecurityStatus SecurityStatus { get; set; }
        public int Size { get; set; }
        public string BuildingId { get; set; }
        public int Floor { get; set; }
        public Point DistanceFromFloorOrigin { get; set; }
        [JsonProperty("Equipment", ItemConverterType = typeof(StringEnumConverter))]
        public List<RoomEquipment> Equipment { get; set; }
    }
}
