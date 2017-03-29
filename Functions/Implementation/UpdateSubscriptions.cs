using System;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Functions.Implementation
{
    public class UpdateSubscriptions
    {
        public static readonly string WebHookUri = ConfigurationManager.AppSettings["WebHookUri"];
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IQueryable<DynamicTableEntity> rooms, CloudTable roomsTable, IQueryable<DynamicTableEntity> serviceConfig)
        {
            log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

            var allRooms = rooms.ToList().GroupBy(i => i.PartitionKey).ToDictionary(i => i.Key, i => i.ToList());
            foreach(var org in allRooms)
            {
                var config = serviceConfig.Where(i => i.PartitionKey == org.Key && i.RowKey == "Exchange").ToList().SingleOrDefault();
                if(null == config)
                {
                    log.Info($"No exchange configuration found for {org.Key} - skipping {org.Value.Count} rooms");
                    continue;
                }

                var configData = JObject.Parse(config["Data"]?.StringValue);
                var clientId = (string)configData["ClientId"];
                var clientCertificate = (string)configData["ClientCertificate"];
                var tenantId = (string)configData["TenantId"];
                if(string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientCertificate) || string.IsNullOrEmpty(tenantId))
                {
                    log.Info($"Missing some exchange configuration for {org.Key} - skipping {org.Value.Count} rooms");
                    continue;
                }

                log.Info("Loading cert");
                var cert = new X509Certificate2();
                cert.Import(Convert.FromBase64String(clientCertificate), string.Empty, X509KeyStorageFlags.MachineKeySet);
                log.Info($"Using cert: {cert.GetPublicKeyString()}");

                var ctx = new AuthenticationContext(Authority + tenantId);
                var result = await ctx.AcquireTokenAsync(OutlookResource, new ClientAssertionCertificate(clientId, cert));
                var authToken = result.AccessToken;

                log.Info("Got access token");

                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

                var baseUri = new Uri("https://outlook.office.com/api/v2.0/");
                using (client)
                {
                    foreach(var room in org.Value)
                    {
                        var subId = room.Properties.ContainsKey("SubscriptionId") ? room["SubscriptionId"]?.StringValue : null;
                        var roomAddress = (string)JObject.Parse(room["Data"]?.StringValue)["RoomAddress"];
                        var roomUri = new Uri(baseUri, $"Users('{roomAddress}')/");
                        if(!string.IsNullOrEmpty(subId))
                        {
                            // extend the current subscription by another day
                            log.Info($"Extending {subId} for {roomAddress}");
                            var obj = JObject.FromObject(new
                            {
                                SubscriptionExpirationDateTime = DateTime.UtcNow.AddDays(1).ToString("o"),
                            });
                            obj["@odata.type"] = "#Microsoft.OutlookServices.PushSubscription";
                            var content = new StringContent(obj.ToString(Formatting.None), Encoding.UTF8, "application/json");

                            var reqH = new HttpRequestMessage(new HttpMethod("PATCH"), new Uri(roomUri, $"subscriptions/{subId}").AbsoluteUri) { Content = content };
                            using (var r = await client.SendAsync(reqH))
                            {
                                if(r.StatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    // nothing further needed
                                    continue;
                                }
                                log.Info($"Unable to renew existing sub {subId} for {roomAddress}: {r.StatusCode}");
                            }
                        }

                        // either there wasn't an existing subscription, or we were unable to renew it - let's create a new one
                        {
                            log.Info($"Creating new subscription for {roomAddress}");
                            var obj = JObject.FromObject(new
                            {
                                Resource = new Uri(roomUri, $"events").AbsoluteUri,
                                NotificationURL = WebHookUri,
                                ChangeType = "Created,Deleted,Updated",
                                ClientState = $"{room.PartitionKey}_{room.RowKey}",
                                SubscriptionExpirationDateTime = DateTime.UtcNow.AddDays(1).ToString("o"),
                            });
                            obj["@odata.type"] = "#Microsoft.OutlookServices.PushSubscription";
                            var content = new StringContent(obj.ToString(Formatting.None), Encoding.UTF8, "application/json");
                            var uri = new Uri(roomUri, $"subscriptions").AbsoluteUri;

                            log.Info($"POSTing to {uri} with {await content.ReadAsStringAsync()}");

                            var reqH = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
                            using (var r = await client.SendAsync(reqH))
                            {
                                r.EnsureSuccessStatusCode();
                                var rObj = JObject.Parse(await r.Content.ReadAsStringAsync());
                                subId = (string)rObj["Id"];
                            }

                            room["SubscriptionId"] = new EntityProperty(subId);
                            await roomsTable.ExecuteAsync(TableOperation.Replace(room));
                            log.Info($"Created subscription {subId} for {roomAddress} and updated room object");
                        }
                    }
                }
            }

            return req.CreateResponse(HttpStatusCode.OK, "");
        }


        public static readonly string OutlookResource = "https://outlook.office.com";
        public static readonly string Authority = "https://login.windows.net/";

    }
}
