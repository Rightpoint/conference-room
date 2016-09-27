using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IOrganizationServiceConfigurationRepository
    {
        OrganizationServiceConfigurationEntity Get(string organizationId, string serviceName);
    }
}