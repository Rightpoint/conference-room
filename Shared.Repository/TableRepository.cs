using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace RightpointLabs.ConferenceRoom.Shared.Repository
{
    public class TableRepository<T>: BaseTableRepository<T> where T : Entity
    {
        public TableRepository(CloudTableClient client) : base(client)
        {
        }

        public override Task Upsert(T item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();
            item.LastModified = DateTime.UtcNow;

            return base.Upsert(item);
        }

        public async Task<T> GetByIdAsync(string id)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionById(id)))).Select(FromTableEntity).SingleOrDefault();
        }

        protected string FilterConditionById(string id)
        {
            return
                TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, id),
                    TableOperators.And,
                    TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id));
        }

        protected override string GetRowKey(T entity)
        {
            return entity.Id;
        }
    }
}