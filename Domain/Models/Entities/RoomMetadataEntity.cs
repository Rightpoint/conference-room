using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class RoomMetadataEntity : Entity, IRoom
    {
        public string RoomAddress { get; set; }
        public int Size { get; set; }
        public string BuildingId { get; set; }
        public string FloorId { get; set; }
        public Point DistanceFromFloorOrigin { get; set; }
        [JsonProperty("Equipment", ItemConverterType = typeof(StringEnumConverter))]
        public List<RoomEquipment> Equipment { get; set; }
        public string GdoDeviceId { get; set; }
        public string BeaconUid { get; set; }
        public string OrganizationId { get; set; }
    }
}
