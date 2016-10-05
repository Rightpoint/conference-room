using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IContextService
    {
        bool IsAuthenticated { get; }
        string UserId { get; }
        DeviceEntity CurrentDevice { get; }
        OrganizationEntity CurrentOrganization { get; }
    }
}