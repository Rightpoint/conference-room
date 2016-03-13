using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class RoomMetadata : Entity
    {
        public int Size { get; set; }
        public string BuildingId { get; set; }
        public int Floor { get; set; }
        public Point DistanceFromFloorOrigin { get; set; }
        [JsonProperty("Equipment", ItemConverterType = typeof(StringEnumConverter))]
        public List<RoomEquipment> Equipment { get; set; }
    }
}
