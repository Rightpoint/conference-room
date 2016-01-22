namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface ISmsMessagingService
    {
        void Send(string[] numbers, string message);
    }
}