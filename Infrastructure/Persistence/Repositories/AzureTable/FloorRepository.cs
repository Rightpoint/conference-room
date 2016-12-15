using RightpointLabs.ConferenceRoom.Domain.Repositories;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class FloorRepository : TableByOrganizationRepository<FloorEntity>, IFloorRepository
    {
        public FloorRepository(CloudTableClient client)
            : base(client)
        {
        }

        public FloorEntity Get(string floorId)
        {
            return this.GetById(floorId);
        }

        public IEnumerable<FloorEntity> GetAllByOrganization(string organizationId)
        {
            return base.GetAll(organizationId);
        }

        public IEnumerable<FloorEntity> GetAllByBuilding(string buildingId)
        {
            return _table.ExecuteQuery(new TableQuery<DynamicTableEntity>().Where(FilterConditionAllByBuilding(buildingId))).Select(FromTableEntity);
        }

        public string FilterConditionAllByBuilding(string buildingId)
        {
            return TableQuery.GenerateFilterCondition("BuildingId", QueryComparisons.Equal, buildingId);
        }

        protected override DynamicTableEntity ToTableEntity(FloorEntity entity)
        {
            var tableEntity = base.ToTableEntity(entity);
            tableEntity.Properties["BuildingId"] = new EntityProperty(entity.BuildingId);
            return tableEntity;
        }
    }
}