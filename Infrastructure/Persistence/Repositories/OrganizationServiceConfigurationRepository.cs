using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class OrganizationServiceConfigurationRepository : EntityRepository<OrganizationServiceConfigurationEntity>, IOrganizationServiceConfigurationRepository
    {
        public OrganizationServiceConfigurationRepository(OrganizationServiceConfigurationEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public OrganizationServiceConfigurationEntity Get(string organizationId, string serviceName)
        {
            var q = Query<OrganizationServiceConfigurationEntity>.Where(i => i.OrganizationId == organizationId && i.ServiceName == serviceName);
            return this.Collection.FindOne(q);
        }
    }
}