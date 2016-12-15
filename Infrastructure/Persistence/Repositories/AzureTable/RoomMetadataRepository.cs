using RightpointLabs.ConferenceRoom.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<RoomMetadataEntity> GetRoomInfosForOrganization(string organizationId)
        {
            return GetAll(organizationId);
        }

        protected override DynamicTableEntity ToTableEntity(RoomMetadataEntity entity)
        {
            var tableEntity = base.ToTableEntity(entity);
            tableEntity.Properties["BuildingId"] = new EntityProperty(entity.BuildingId);
            return tableEntity;
        }
    }
}