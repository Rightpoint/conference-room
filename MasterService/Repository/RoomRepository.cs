using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace MasterService.Repository
{
    public class RoomRepository : TableByOrganizationRepository<Room>
    {
        public RoomRepository(CloudTableClient client)
            : base(client)
        {
        }

        public async Task<IEnumerable<Room>> GetAllByBuildingAsync(string organizationId, string buildingId)
        {
            var q = new TableQuery<DynamicTableEntity>().Where(
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, organizationId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("BuildingId", QueryComparisons.Equal, buildingId))
            );
            return (await _table.ExecuteQueryAsync(q)).Select(FromTableEntity);
        }

        protected override DynamicTableEntity ToTableEntity(Room entity)
        {
            var tableEntity = base.ToTableEntity(entity);
            tableEntity.Properties["BuildingId"] = new EntityProperty(entity.BuildingId);
            return tableEntity;
        }
    }
}