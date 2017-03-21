namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public interface IExchangeRestChangeNotificationService
    {
        void TrackOrganization(string organizationId);
        void UpdateRooms(string organizationId);
        bool IsTrackedForChanges(string organizationId);
    }
}