using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace RightpointLabs.ConferenceRoom.Functions.Implementation
{
    public class HandleSubscriptionEvent
    {
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, CloudTable rooms, IAsyncCollector<string> topic)
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
            if(string.IsNullOrEmpty(clientState))
            {
                log.Info("Missing clientState");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Missing clientState");
            }

            var parts = clientState.Split('_');
            if(parts.Length != 2)
            {
                log.Info("Invalid clientState");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid clientState");
            }

            var orgId = parts[0];
            var roomId = parts[1];
            var roomEntity = rooms.CreateQuery<DynamicTableEntity>().Where(i => i.PartitionKey == orgId && i.RowKey == roomId).SingleOrDefault();
            if(null == roomEntity)
            {
                log.Info("Cannot find room");
                return req.CreateResponse(HttpStatusCode.BadRequest, "Cannot find room");
            }

            var room = JObject.Parse(roomEntity["Data"]?.StringValue);
            var subId = roomEntity.Properties.ContainsKey("SubscriptionId") ? roomEntity["SubscriptionId"]?.StringValue : null;
            if(string.IsNullOrEmpty(subId))
            {
                log.Info("No active subscription for room");
                return req.CreateResponse(HttpStatusCode.BadRequest, "No active subscription for room");
            }

            var lastBroadcast = roomEntity.Properties.ContainsKey("LastBroadcastEtag") ? roomEntity["LastBroadcastEtag"]?.StringValue : null;

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
                            var etag = (notification["ResourceData"] as JObject)?["@odata.etag"]?.Value<string>();
                            if (!string.IsNullOrEmpty(lastBroadcast) && !string.IsNullOrEmpty(etag) && etag == lastBroadcast)
                            {
                                log.Info($"Ignoring duplicate broadcast of {etag} on {room}");
                                break;
                            }

                            log.Info($"Broadcasting {notification} on {room}");
                            await topic.AddAsync(JObject.FromObject(new { notification, room }).ToString());
                            if (!string.IsNullOrEmpty(etag))
                            {
                                roomEntity.Properties["LastBroadcastEtag"] = new EntityProperty(etag);
                                await rooms.ExecuteAsync(TableOperation.Replace(roomEntity));
                                log.Info($"Updated etag on {room} to {etag}");
                            }
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

    }

}
