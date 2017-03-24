#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage;
#r "Newtonsoft.Json"
using Newtonsoft.Json;
#r "Microsoft.ServiceBus"
using System.Net;
using System.Resources;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IQueryable<DynamicTableEntity> rooms, ICollection<JObject> topic)
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

    var clientState = req.Headers["ClientState"];
    if(string.IsNullOrEmpty(clientState))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Missing clientState");
    }

    var parts = clientState.Split("_");
    if(parts.Length != 2)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid clientState");
    }
    var orgId = parts[0];
    var roomId = parts[1];
    var roomEntity = rooms.Where(i => i.PartitionKey == orgId && i.RowKey == roomId).SingleOrDefault();
    if(null == room)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "Cannot find room");
    }
    var room = JObject.Parse(roomEntity.Data);
    var subId = (string)room["SubscriptionId"];
    if(string.IsNullOrEmpty(subId))
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, "No active subscription for room");
    }

    var data = JObject.Parse(await req.Content.ReadAsStringAsync());
    foreach(JObject notification in (JArray)data["value"])
    {
        var nSubId = (string)notification["SubscriptionId"];
        var nType = (string)notification["@odata.type"];
        switch (nType)
        {
            case "#Microsoft.OutlookServices.Notification":
                if(subId == nSubId)
                {
                    var obj = notification.ToObject<NotificationObject>();
                    topic.Add(new { notification, room });
                }
                else
                {
                    log.WriteLine("Invalid subscription id: {0} != {1}", subId, nSubId);
                }
                break;
            default:
                log.WriteLine("Unknown notification type: {0}", nType);
                break;
        }
    }
}
