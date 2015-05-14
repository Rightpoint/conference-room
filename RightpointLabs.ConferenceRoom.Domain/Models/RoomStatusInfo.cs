namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class RoomStatusInfo
    {
        public RoomStatus Status { get; set; }
        public double NextChangeSeconds { get; set; }
        public Meeting Meeting { get; set; }
    }
}