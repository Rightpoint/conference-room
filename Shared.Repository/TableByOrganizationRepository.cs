using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace RightpointLabs.ConferenceRoom.Shared.Repository
{
    public class TableByOrganizationRepository<T> : TableRepository<T> where T : Entity, IByOrganizationId
    {
        public TableByOrganizationRepository(CloudTableClient client) : base(client)
        {
        }
        
        public virtual async Task<IEnumerable<T>> GetAllAsync(string organizationId)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionAll(organizationId)))).Select(FromTableEntity);
        }

        public virtual string FilterConditionAll(string organizationId)
        {
            return
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, organizationId);

        }

        public virtual async Task<T> GetByIdAsync(string organizationId, string id)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionById(organizationId, id)))).Select(FromTableEntity).SingleOrDefault();
        }

        protected virtual string FilterConditionById(string organizationId, string id)
        {
            return
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, organizationId),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id));
        }
        
        public virtual async Task<IEnumerable<T>> GetByIdAsync(string organizationId, string[] id)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionById(organizationId, id)))).Select(FromTableEntity);
        }

        protected virtual string FilterConditionById(string organizationId, string[] id)
        {
            return
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, organizationId),
                    TableOperators.And,
                    id.Aggregate("", (q, i) =>
                    {
                        var part = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, i);
                        if (string.IsNullOrEmpty(q))
                            return part;
                        return TableQuery.CombineFilters(q, TableOperators.Or, part);
                    }));

        }

        protected override string GetPartitionKey(T entity)
        {
            return entity.OrganizationId;
        }
    }
}