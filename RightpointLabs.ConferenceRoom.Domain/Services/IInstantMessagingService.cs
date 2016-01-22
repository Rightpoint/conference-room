namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IInstantMessagingService
    {
        void SendMessage(string[] targets, string subject, string message, InstantMessagePriority priority);
    }
}