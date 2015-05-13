namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class BaseObject
    {
        public string Address { get; set; }
        public string Name { get; set; }
    }

    public class Room : BaseObject
    {
    }

    public class RoomList : BaseObject
    {
    }
}