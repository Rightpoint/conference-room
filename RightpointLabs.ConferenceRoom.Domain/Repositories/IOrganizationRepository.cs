using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IOrganizationRepository
    {
        OrganizationEntity Get(string organizationId);

        OrganizationEntity GetByUserDomain(string userDomain);
    }
}