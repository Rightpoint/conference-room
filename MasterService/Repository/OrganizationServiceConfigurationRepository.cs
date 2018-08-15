using MasterService.Models;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace MasterService.Repository
{
    public class OrganizationServiceConfigurationRepository : TableByOrganizationRepository<OrganizationServiceConfiguration>
    {
        public OrganizationServiceConfigurationRepository(CloudTableClient client)
            : base(client)
        {
        }

        protected override string GetRowKey(OrganizationServiceConfiguration entity)
        {
            return entity.ServiceName;
        }
    }
}