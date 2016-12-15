namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public interface IRoom : IByOrganizationId
    {
        string Id { get; }
        string RoomAddress { get; }
        string BuildingId { get; }
        string FloorId { get; }
    }
}
