using RightpointLabs.ConferenceRoom.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class RoomMetadataRepository : TableByOrganizationRepository<RoomMetadataEntity>, IRoomMetadataRepository
    {
        public RoomMetadataRepository(CloudTableClient client)
            : base(client)
        {
        }

        public RoomMetadataEntity GetRoomInfo(string roomId)
        {
            return this.GetById(roomId);
        }

        public IEnumerable<RoomMetadataEntity> GetRoomInfosForBuilding(string buildingId)
        {
            return _table.ExecuteQuery(new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("BuildingId", QueryComparisons.Equal, buildingId))).Select(FromTableEntity);
        }

        public async Task<IEnumerable<RoomMetadataEntity>> GetRoomInfosForBuildingAsync(string buildingId)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(TableQuery.GenerateFilterCondition("BuildingId", QueryComparisons.Equal, buildingId)))).Select(FromTableEntity);
        }

        public IEnumerable<RoomMetadataEntity> GetRoomInfosForOrganization(string organizationId)
        {
            return GetAll(organizationId);
        }

        public Task<IEnumerable<RoomMetadataEntity>> GetRoomInfosForOrganizationAsync(string organizationId)
        {
            return GetAllAsync(organizationId);
        }

        protected override DynamicTableEntity ToTableEntity(RoomMetadataEntity entity)
        {
            var tableEntity = base.ToTableEntity(entity);
            tableEntity.Properties["BuildingId"] = new EntityProperty(entity.BuildingId);
            return tableEntity;
        }
    }
}