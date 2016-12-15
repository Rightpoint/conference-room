using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class DeviceStatusRepository : IDeviceStatusRepository
    {
        private readonly CloudTable _table;

        public DeviceStatusRepository(CloudTableClient client)
        {
            _table = client.GetTableReference("DeviceStatusEntity");
            _table.CreateIfNotExists();
        }
        
        public void Insert(DeviceStatus status)
        {
            _table.Execute(TableOperation.Insert(new DeviceStatusEntity(status)));
        }

        public IEnumerable<DeviceStatus> GetRange(string organizationId, DateTime start, DateTime end)
        {
            var q = new TableQuery<DeviceStatusEntity>().Where(BuildPartitionRowFilter(organizationId, start, end));
            return 
                _table.ExecuteQuery(q)
                    .Select(i => i.ToModel());
        }


        public IEnumerable<DeviceStatus> GetRange(string organizationId, string deviceId, DateTime start, DateTime end)
        {
            var prFilter = BuildPartitionRowFilter(organizationId, start, end);
            var deviceIdFilter = TableQuery.GenerateFilterCondition("DeviceId", QueryComparisons.Equal, deviceId);
            var q = new TableQuery<DeviceStatusEntity>().Where(TableQuery.CombineFilters(prFilter, TableOperators.And, deviceIdFilter));
            return
                _table.ExecuteQuery(q)
                    .Select(i => i.ToModel());
        }

        private string BuildPartitionRowFilter(string organizationId, DateTime start, DateTime end)
        {
            var startPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.LessThanOrEqual, MakePartitionKey(organizationId, start));
            var endPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThanOrEqual, MakePartitionKey(organizationId, end.AddHours(1)));
            var startRowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThanOrEqual, MakeRowKey(start));
            var endRowFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThan, MakeRowKey(end));
            var filter = TableQuery.CombineFilters(
                TableQuery.CombineFilters(startPartitionFilter, TableOperators.And, endPartitionFilter),
                TableOperators.And,
                TableQuery.CombineFilters(startRowFilter, TableOperators.And, endRowFilter));
            return filter;
        }

        public class DeviceStatusEntity : TableEntity
        {
            public string OrganizationId { get; set; }
            public string DeviceId { get; set; }
            public DateTime StatusTimestamp { get; set; }
            public double? Temperature1 { get; set; }
            public double? Temperature2 { get; set; }
            public double? Temperature3 { get; set; }
            public double? Voltage1 { get; set; }
            public double? Voltage2 { get; set; }
            public double? Voltage3 { get; set; }

            public DeviceStatusEntity()
            {
            }

            public DeviceStatusEntity(DeviceStatus model)
            {
                this.OrganizationId = model.OrganizationId;
                this.DeviceId = model.DeviceId;
                this.StatusTimestamp = model.StatusTimestamp;
                this.Temperature1 = model.Temperature1;
                this.Temperature2 = model.Temperature2;
                this.Temperature3 = model.Temperature3;
                this.Voltage1 = model.Voltage1;
                this.Voltage2 = model.Voltage2;
                this.Voltage3 = model.Voltage3;

                this.PartitionKey = MakePartitionKey(this.OrganizationId, this.StatusTimestamp);
                this.RowKey = MakeUniqueRowKey(this.StatusTimestamp);
            }

            public DeviceStatus ToModel()
            {
                return new DeviceStatus()
                {
                    OrganizationId = this.OrganizationId,
                    DeviceId = this.DeviceId,
                    StatusTimestamp = this.StatusTimestamp,
                    Temperature1 = this.Temperature1,
                    Temperature2 = this.Temperature2,
                    Temperature3 = this.Temperature3,
                    Voltage1 = this.Voltage1,
                    Voltage2 = this.Voltage2,
                    Voltage3 = this.Voltage3,
                };
            }
        }

        private static string MakePartitionKey(string orgId, DateTime time)
        {
            return string.Format(
                "{0}.{1:D19}",
                orgId,
                 DateTime.MaxValue.Ticks - (time.Date.AddHours(time.Hour)).Ticks);
        }

        private static string MakeUniqueRowKey(DateTime time)
        {
            return string.Format(
                "{0:D19}.{1}",
                 DateTime.MaxValue.Ticks - time.Ticks,
                 Guid.NewGuid().ToString().ToLower());
        }

        private static string MakeRowKey(DateTime time)
        {
            return string.Format(
                "{0:D19}",
                DateTime.MaxValue.Ticks - time.Ticks);
        }
    }
}