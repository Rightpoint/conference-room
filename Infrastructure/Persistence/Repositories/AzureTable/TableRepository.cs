using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class TableRepository<T> where T : Entity
    {
        protected readonly CloudTable _table;

        protected virtual string TableName => typeof(T).Name;

        public TableRepository(CloudTableClient client)
        {
            _table = client.GetTableReference(TableName);
            _table.CreateIfNotExists();
        }

        public void Insert(T item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();
            item.LastModified = DateTime.UtcNow;

            _table.Execute(TableOperation.Insert(ToTableEntity(item)));
        }

        public void Update(T item)
        {
            item.LastModified = DateTime.UtcNow;
            _table.Execute(TableOperation.Replace(ToTableEntity(item)));
        }

        public void Upsert(T item)
        {
            if (string.IsNullOrEmpty(item.Id))
                item.Id = Guid.NewGuid().ToString();
            item.LastModified = DateTime.UtcNow;

            _table.Execute(TableOperation.InsertOrReplace(ToTableEntity(item)));
        }

        public T GetById(string id)
        {
            return _table.ExecuteQuery(new TableQuery<DynamicTableEntity>().Where(FilterConditionById(id))).Select(FromTableEntity).SingleOrDefault();
        }

        public IEnumerable<T> GetAll()
        {
            return _table.ExecuteQuery(new TableQuery<DynamicTableEntity>()).Select(FromTableEntity);
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

            var tableEntity = new DynamicTableEntity(GetPartitionKey(entity), GetRowKey(entity));
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
    }
}