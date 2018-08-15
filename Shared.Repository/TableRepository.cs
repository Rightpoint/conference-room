using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Shared.Repository
{
    public class TableRepository<T> where T : Entity
    {
        protected readonly CloudTable _table;

        protected virtual string TableName => typeof(T).Name;

        public TableRepository(CloudTableClient client)
        {
            _table = client.GetTableReference(TableName);
        }

        public Task Insert(T item)
        {
            return Upsert(item);
        }

        public Task Update(T item)
        {
            return Upsert(item);
        }

        public async Task Upsert(T item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();
            item.LastModified = DateTime.UtcNow;

            await _table.ExecuteAsync(TableOperation.InsertOrReplace(ToTableEntity(item)));
        }

        public async Task<T> GetByIdAsync(string id)
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>().Where(FilterConditionById(id)))).Select(FromTableEntity).SingleOrDefault();
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>())).Select(FromTableEntity);
        }

        protected string FilterConditionById(string id)
        {
            return TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, id);
        }
        
        protected virtual string GetPartitionKey(T entity)
        {
            return "";
        }

        protected virtual string GetRowKey(T entity)
        {
            return entity.Id;
        }

        protected virtual DynamicTableEntity ToTableEntity(T entity)
        {
            if (null == entity)
                return null;

            var tableEntity = new DynamicTableEntity(GetPartitionKey(entity), GetRowKey(entity)) { ETag = "*" };
            tableEntity.Properties.Add("Data", new EntityProperty(BuildJObject(entity).ToString(Formatting.None)));
            return tableEntity;
        }

        protected virtual JObject BuildJObject(T entity)
        {
            return JObject.FromObject(entity);
        }

        protected virtual T FromTableEntity(DynamicTableEntity tableEntity)
        {
            if (null == tableEntity)
                return null;

            return JObject.Parse(tableEntity.Properties["Data"].StringValue).ToObject<T>();
        }

        public Task InitAsync()
        {
            return _table.CreateIfNotExistsAsync();
        }
    }
}