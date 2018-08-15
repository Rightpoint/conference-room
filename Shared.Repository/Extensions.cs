using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace RightpointLabs.ConferenceRoom.Shared.Repository
{
    public static class Extensions
    {
        public static async Task<IList<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            var data = new List<T>();
            TableContinuationToken tableContinuationToken = null;
            do
            {
                var queryResponse = await table.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
                tableContinuationToken = queryResponse.ContinuationToken;
                data.AddRange(queryResponse.Results);
            }
            while (tableContinuationToken != null);

            return data;
        }
    }
}
