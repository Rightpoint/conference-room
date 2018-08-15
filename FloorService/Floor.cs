using System.Drawing;
using RightpointLabs.ConferenceRoom.Shared;

namespace FloorService
{
    public class Floor : Entity, IByOrganizationId
    {
        public string OrganizationId { get; set; }
        public string BuildingId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public Rectangle CoordinatesInImage { get; set; }
    }
}
