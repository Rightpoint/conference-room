#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"
#r "Microsoft.ServiceBus"
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Resources;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IQueryable<DynamicTableEntity> rooms, IAsyncCollector<string> topic)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    // parse query parameter
    string validationToken = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "validationToken", true) == 0)
        .Value;

    if (!string.IsNullOrEmpty(validationToken))
    {
        return req.CreateResponse(HttpStatusCode.OK, validationToken);
    }

    var clientState = req.Headers.GetValues("ClientState").FirstOrDefault();
    if (string.IsNullOrEmpty(clientState))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Missing clientState");
    }

    var parts = clientState.Split(' ');
    if (parts.Length != 2)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid clientState");
    }

    var orgId = parts[0];
    var roomId = parts[1];
    var roomEntity = rooms.Where(i => i.PartitionKey == orgId && i.RowKey == roomId).SingleOrDefault();
    if (null == roomEntity)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Cannot find room");
    }

    var room = JObject.Parse(roomEntity["Data"]?.StringValue);
    var subId = (string)room["SubscriptionId"];
    if (string.IsNullOrEmpty(subId))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "No active subscription for room");
    }

    var data = JObject.Parse(await req.Content.ReadAsStringAsync());
    foreach (JObject notification in (JArray)data["value"])
    {
        var nSubId = (string)notification["SubscriptionId"];
        var nType = (string)notification["@odata.type"];
        switch (nType)
        {
            case "#Microsoft.OutlookServices.Notification":
                if (subId == nSubId)
                {
                    await topic.AddAsync(JObject.FromObject(new { notification, room }).ToString());
                }
                else
                {
                    log.Info($"Invalid subscription id: {subId} != {nSubId}");
                }
                break;
            default:
                log.Info($"Unknown notification type: {nType}");
                break;
        }
    }

    return req.CreateResponse(HttpStatusCode.OK, "");
}
