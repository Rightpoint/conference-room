using Microsoft.WindowsAzure.Storage.Table;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class OrganizationServiceConfigurationRepository : TableByOrganizationRepository<OrganizationServiceConfigurationEntity>, IOrganizationServiceConfigurationRepository
    {
        public OrganizationServiceConfigurationRepository(CloudTableClient client)
            : base(client)
        {
        }

        public OrganizationServiceConfigurationEntity Get(string organizationId, string serviceName)
        {
            return GetById(organizationId, serviceName);
        }

        protected override string GetRowKey(OrganizationServiceConfigurationEntity entity)
        {
            return entity.ServiceName;
        }
    }
}