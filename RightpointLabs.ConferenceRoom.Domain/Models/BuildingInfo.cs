using System.Collections.Generic;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class BuildingInfo : Entity
    {
        public string Name { get; set; }
        public string StreetAddress1 { get; set; }
        public string StreetAddress2 { get; set; }
        public string City { get; set; }
        public string StateOrProvence { get; set; }
        public string PostalCode { get; set; }
        public List<FloorInfo> Floors { get; set; }
    }
}
