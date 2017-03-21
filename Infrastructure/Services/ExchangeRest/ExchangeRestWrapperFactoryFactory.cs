using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestWrapperFactoryFactory
    {
        public ExchangeRestWrapperFactory GetFactory(OrganizationEntity org, string clientId, string clientSecret, string username, string password, string defaultUser)
        {
            return new UserExchangeRestWrapperFactory(clientId, clientSecret, username, password, defaultUser);
        }

        public ExchangeRestWrapperFactory GetFactory(OrganizationEntity org, string tenantId, string clientId, string clientCertificate, string defaultUser)
        {
            return new AppOnlyExchangeRestWrapperFactory(tenantId, clientId, clientCertificate, defaultUser);
        }

        public abstract class ExchangeRestWrapperFactory
        {
            public static readonly string OutlookResource = "https://outlook.office.com";
            public static readonly string GraphResource = "https://graph.microsoft.com";

            protected abstract Task<string> GetAccessTokenFor(string resource);

            protected string _defaultUser;

            protected virtual void Update(HttpClient client)
            {
            }

            public async Task<ExchangeRestWrapper> CreateExchange()
            {
                var token = await GetAccessTokenFor(OutlookResource);
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var longClient = new HttpClient();
                longClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                longClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                Update(client);
                Update(longClient);

                return new ExchangeRestWrapper(client, longClient, _defaultUser);
            }

            public async Task<GraphRestWrapper> CreateGraph()
            {
                var token = await GetAccessTokenFor(GraphResource);
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var longClient = new HttpClient();
                longClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                longClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                Update(client);
                Update(longClient);

                return new GraphRestWrapper(client, longClient);
            }
        }

        public class AppOnlyExchangeRestWrapperFactory : ExchangeRestWrapperFactory
        {
            private readonly string _tenantId;
            private readonly string _clientId;
            private readonly string _clientCertificate;
            public static readonly string Authority = "https://login.windows.net/";

            public AppOnlyExchangeRestWrapperFactory(string tenantId, string clientId, string clientCertificate, string defaultUser)
            {
                _tenantId = tenantId;
                _clientId = clientId;
                _clientCertificate = clientCertificate;
                _defaultUser = defaultUser;
            }

            protected override void Update(HttpClient client)
            {
                client.DefaultRequestHeaders.Add("UserAgent", "Rightpoint/RoomNinja/0.1");
                client.DefaultRequestHeaders.Add("client-request-id", Guid.NewGuid().ToString());
                client.DefaultRequestHeaders.Add("return-client-request-id", "true");
            }

            protected override async Task<string> GetAccessTokenFor(string resource)
            {
                var cert = new X509Certificate2();
                cert.Import(Convert.FromBase64String(_clientCertificate));

                var c = new AuthenticationContext(Authority + _tenantId);
                var r = await c.AcquireTokenAsync(resource, new ClientAssertionCertificate(_clientId, cert));
                return r.AccessToken;
            }
        }

        public class UserExchangeRestWrapperFactory : ExchangeRestWrapperFactory
        {
            private readonly string _clientId;
            private readonly string _clientSecret;
            private readonly string _username;
            private readonly string _password;
            public static readonly string Authority = "https://login.windows.net/common/oauth2/token";

            public UserExchangeRestWrapperFactory(string clientId, string clientSecret, string username, string password, string defaultUser)
            {
                _clientId = clientId;
                _clientSecret = clientSecret;
                _username = username;
                _password = password;
                _defaultUser = defaultUser;
            }

            protected override async Task<string> GetAccessTokenFor(string resource)
            {
                // todo: support token lifetime, refresh tokens, etc.
                var vals = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "password"),
                    new KeyValuePair<string, string>("scope", "openid"),
                    new KeyValuePair<string, string>("resource", resource),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                    new KeyValuePair<string, string>("username", _username),
                    new KeyValuePair<string, string>("password", _password)
                };

                using (HttpClient hc = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(vals);
                    var hrm = await hc.PostAsync(Authority, content).ConfigureAwait(false);
                    hrm.EnsureSuccessStatusCode();
                    var response = JObject.Parse(await hrm.Content.ReadAsStringAsync()).ToObject<LoginResponse>();
                    var at = response.access_token;

                    return at;
                }
            }

            private class LoginResponse
            {
                public string id_token { get; set; }
                public string access_token { get; set; }
                public string refresh_token { get; set; }
                public int expires_in { get; set; }
                public int expires_on { get; set; }
            }
        }
        
    }
}
