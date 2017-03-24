using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

public static readonly string WebHookUri = ConfigurationManager.AppSettings["WebHookUri"];
public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log, IQueryable<DynamicTableEntity> rooms, CloudTable roomsTable, IQueryable<DynamicTableEntity> serviceConfig)
{
    log.Info($"C# HTTP trigger function processed a request. RequestUri={req.RequestUri}");

    var allRooms = rooms.GroupBy(i => i.PartitionKey).ToDictionary(i => i.Key, i => i.ToList());
    foreach(var org in allRooms)
    {
        var config = serviceConfig.SingleOrDefault(i => i.PartitionKey == org.Key && i.RowKey == "Exchange");
        if(null != config)
        {
            log.Info($"No exchange configuration found for {org.Key}- skipping {org.Value.Count} rooms");
            continue;
        }

        var clientId = (string)config["ClientId"];
        var clientCertificate = (string)config["ClientCertificate"];
        var tenantId = (string)config["TenantId"];
        if(string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientCertificate) || string.IsNullOrEmpty(tenantId))
        {
            log.Info($"Missing some exchange configuration for {org.Key}- skipping {org.Value.Count} rooms");
            continue;
        }

        var cert = new X509Certificate2();
        cert.Import(Convert.FromBase64String(clientCertificate));

        var ctx = new AuthenticationContext(Authority + tenantId);
        var result = await ctx.AcquireTokenAsync(OutlookResource, new ClientAssertionCertificate(clientId, cert));
        var authToken = result.AccessToken;

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var baseUri = new Uri("https://outlook.office.com/api/v2.0/");
        using (client)
        {
            foreach(var room in org.Value)
            {
                var subId = (string)room["SubscriptionId"];
                var roomAddress = (string)JObject.Parse((string)room["Data"])["RoomAddress"];
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

                    var req = new HttpRequestMessage(HttpMethod.Patch, new Uri(roomUri, $"subscriptions/{subId}").AbsoluteUri) { Content = content };
                    using (var r = await client.SendAsync(req))
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

                    var req = new HttpRequestMessage(HttpMethod.Post, new Uri(roomUri, $"subscriptions").AbsoluteUri) { Content = content };
                    using (var r = await client.SendAsync(req))
                    {
                        r.EnsureSuccessStatusCode();
                        var rObj = JObject.Parse(await r.Content.ReadAsStringAsync());
                        subId = (string)rObj["Id"];
                    }

                    room["SubscriptionId"] = subId;
                    await roomsTable.ExecuteAsync(TableOperation.Replace(room));
                    log.Info($"Created subscription {subId} for {roomAddress} and updated room object");
                }
            }
        }
    }
}


public static readonly string OutlookResource = "https://outlook.office.com";
public static readonly string Authority = "https://login.windows.net/";