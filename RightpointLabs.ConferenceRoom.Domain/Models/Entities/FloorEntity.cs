using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class FloorEntity : ICloneable
    {
        public int Floor { get; set; }
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
                Floor = this.Floor,
                Name = this.Name,
                Image = this.Image,
                CoordinatesInImage = (this.CoordinatesInImage ?? new Rectangle()).Clone(),
            };
        }
    }
}
