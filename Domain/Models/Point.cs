using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class Point : ICloneable
    {
        public double X { get; set; }
        public double Y { get; set; }

        object ICloneable.Clone()
        {
            return this.Clone();
        }

        public Point Clone()
        {
            return new Point()
            {
                X = this.X,
                Y = this.Y,
            };
        }
    }
}
