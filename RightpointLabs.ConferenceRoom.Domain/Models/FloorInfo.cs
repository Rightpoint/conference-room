using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class FloorInfo : Entity, ICloneable
    {
        public string Name { get; set; }
        public string Image { get; set; }
        public Rectangle CoordinatesInImage { get; set; }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public FloorInfo Clone()
        {
            return new FloorInfo()
            {
                Id = this.Id,
                Name = this.Name,
                Image = this.Image,
                CoordinatesInImage = (this.CoordinatesInImage ?? new Rectangle()).Clone(),
            };
        }
    }
}
