namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IChangeNotificationService
    {
        void TrackRoom(string roomAddress);
        bool IsTrackedForChanges(string roomAddress);
    }
}