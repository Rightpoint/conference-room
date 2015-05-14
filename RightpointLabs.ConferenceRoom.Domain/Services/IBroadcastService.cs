namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IBroadcastService
    {
        void BroadcastUpdate(string roomAddress);
    }
}