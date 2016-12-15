using RightpointLabs.ConferenceRoom.Domain.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class BuildingRepository : TableByOrganizationRepository<BuildingEntity>, IBuildingRepository
    {
        public BuildingRepository(CloudTableClient client)
            : base(client)
        {
        }

        public BuildingEntity Get(string buildingId)
        {
            return this.GetById(buildingId);
        }
    }
}