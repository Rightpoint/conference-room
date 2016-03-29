using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class Rectangle : ICloneable
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Bottom { get; set; }
        public double Right { get; set; }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public Rectangle Clone()
        {
            return new Rectangle()
            {
                Top = this.Top,
                Left = this.Left,
                Bottom = this.Bottom,
                Right = this.Right,
            };
        }
    }
}
