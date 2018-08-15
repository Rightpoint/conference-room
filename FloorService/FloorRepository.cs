using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace FloorService
{
    public class FloorRepository : TableByOrganizationRepository<Floor>
    {
        public FloorRepository(CloudTableClient client)
            : base(client)
        {
        }
        
        public async Task<IEnumerable<Floor>> GetAllByBuildingAsync(string buildingId)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionAllByBuilding(buildingId)))).Select(FromTableEntity);
        }

        protected string FilterConditionAllByBuilding(string buildingId)
        {
            return TableQuery.GenerateFilterCondition("BuildingId", QueryComparisons.Equal, buildingId);
        }

        protected override DynamicTableEntity ToTableEntity(Floor entity)
        {
            var tableEntity = base.ToTableEntity(entity);
            tableEntity.Properties["BuildingId"] = new EntityProperty(entity.BuildingId);
            return tableEntity;
        }
    }
}