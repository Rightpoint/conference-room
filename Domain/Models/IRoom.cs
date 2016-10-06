namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public interface IRoom
    {
        string Id { get; }
        string RoomAddress { get; }
        string BuildingId { get; }
        string FloorId { get; }
    }
}
