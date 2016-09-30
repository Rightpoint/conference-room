using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class BuildingEntity : Entity
    {
        public string Name { get; set; }
        public string StreetAddress1 { get; set; }
        public string StreetAddress2 { get; set; }
        public string City { get; set; }
        public string StateOrProvence { get; set; }
        public string PostalCode { get; set; }
        public List<FloorEntity> Floors { get; set; }
        public string[] RoomListAddresses { get; set; }
    }
}
