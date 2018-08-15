using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace BuildingService
{
    public class BuildingRepository : TableByOrganizationRepository<Building>
    {
        public BuildingRepository(CloudTableClient client)
            : base(client)
        {
        }
    }
}