using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class FloorEntity : Entity, IByOrganizationId
    {
        public string OrganizationId { get; set; }
        public string BuildingId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public Rectangle CoordinatesInImage { get; set; }
    }
}
