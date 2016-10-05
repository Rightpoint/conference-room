using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class FloorEntity : Entity, ICloneable
    {
        public string OrganizationId { get; set; }
        public string BuildingId { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public Rectangle CoordinatesInImage { get; set; }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public FloorEntity Clone()
        {
            return new FloorEntity()
            {
                Id = this.Id,
                Name = this.Name,
                Image = this.Image,
                CoordinatesInImage = (this.CoordinatesInImage ?? new Rectangle()).Clone(),
            };
        }
    }
}
