using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace MasterService.Repository
{
    public class DeviceStatusRepository : BaseTableRepository<DeviceStatus>
    {
        public DeviceStatusRepository(CloudTableClient client) : base(client)
        {
        }
        
        public async Task<IEnumerable<DeviceStatus>> GetRangeAsync(string organizationId, DateTimeOffset start, DateTimeOffset end)
        {
            var q = new TableQuery<DynamicTableEntity>().Where(BuildPartitionRowFilter(organizationId, start, end));
            return (await _table.ExecuteQueryAsync(q)).Select(FromTableEntity);
        }


        public async Task<IEnumerable<DeviceStatus>> GetRangeAsync(string organizationId, string deviceId, DateTimeOffset start, DateTimeOffset end)
        {
            var prFilter = BuildPartitionRowFilter(organizationId, start, end);
            var deviceIdFilter = TableQuery.GenerateFilterCondition("DeviceId", QueryComparisons.Equal, deviceId);
            var q = new TableQuery<DynamicTableEntity>().Where(TableQuery.CombineFilters(prFilter, TableOperators.And, deviceIdFilter));
            return (await _table.ExecuteQueryAsync(q)).Select(FromTableEntity);
        }

        private string BuildPartitionRowFilter(string organizationId, DateTimeOffset start, DateTimeOffset end)
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

        protected override string GetPartitionKey(DeviceStatus entity)
        {
            return MakePartitionKey(entity.OrganizationId, entity.StatusTimestamp);
        }

        protected override string GetRowKey(DeviceStatus entity)
        {
            return MakeUniqueRowKey(entity.StatusTimestamp);
        }

        private static string MakePartitionKey(string orgId, DateTimeOffset time)
        {
            return string.Format(
                "{0}.{1:D19}",
                orgId,
                 DateTime.MaxValue.Ticks - (time.Date.AddHours(time.Hour)).Ticks);
        }

        private static string MakeUniqueRowKey(DateTimeOffset time)
        {
            return string.Format(
                "{0:D19}.{1}",
                 DateTime.MaxValue.Ticks - time.Ticks,
                 Guid.NewGuid().ToString().ToLower());
        }

        private static string MakeRowKey(DateTimeOffset time)
        {
            return string.Format(
                "{0:D19}",
                DateTime.MaxValue.Ticks - time.Ticks);
        }
    }
}