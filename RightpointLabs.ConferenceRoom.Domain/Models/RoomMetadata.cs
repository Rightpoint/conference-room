using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public enum RoomEquipment
    {
        None = 0x00,
        Television = 0x01,
        Projector = 0x02,
        Monitor = 0x04,
        Telephone = 0x08,
        Ethernet = 0x10,
    }

    public class RoomMetadata : Entity
    {
        public int Size { get; set; }
        public string BuildingId { get; set; }
        public int Floor { get; set; }
        public Point DistanceFromFloorOrigin { get; set; }
        [JsonProperty("Equipment", ItemConverterType = typeof(StringEnumConverter))]
        public List<RoomEquipment> Equipment { get; set; }
    }

    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
