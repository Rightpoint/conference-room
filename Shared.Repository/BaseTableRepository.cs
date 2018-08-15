using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Shared.Repository
{
    public abstract class BaseTableRepository<T> where T: class
    {
        protected readonly CloudTable _table;

        protected virtual string TableName => typeof(T).Name;

        protected BaseTableRepository(CloudTableClient client)
        {
            _table = client.GetTableReference(TableName);
        }

        public virtual async Task Upsert(T item)
        {
            await _table.ExecuteAsync(TableOperation.InsertOrReplace(ToTableEntity(item)));
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return (await _table.ExecuteQueryAsync(new TableQuery<DynamicTableEntity>())).Select(FromTableEntity);
        }

        protected virtual string GetPartitionKey(T entity)
        {
            return GetRowKey(entity);
        }

        protected abstract string GetRowKey(T entity);

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

        public virtual Task InitAsync()
        {
            return _table.CreateIfNotExistsAsync();
        }
    }
}