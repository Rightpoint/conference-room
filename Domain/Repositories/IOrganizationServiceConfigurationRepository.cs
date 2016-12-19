using System.Collections.Generic;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IOrganizationServiceConfigurationRepository : IRepository
    {
        OrganizationServiceConfigurationEntity Get(string organizationId, string serviceName);
        IEnumerable<OrganizationServiceConfigurationEntity> GetAll(string organizationId);
        void Insert(OrganizationServiceConfigurationEntity model);
        void Update(OrganizationServiceConfigurationEntity model);
    }
}