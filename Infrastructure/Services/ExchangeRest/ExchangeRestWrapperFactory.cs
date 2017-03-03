using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestWrapperFactory
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        public static readonly string OutlookResource = "https://outlook.office.com";
        public static readonly string GraphResource = "https://graph.microsoft.com";
        public static readonly string Authority = "https://login.windows.net/common/oauth2/token";

        public ExchangeRestWrapperFactory(string clientId, string clientSecret)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public async Task<ExchangeRestWrapper> CreateExchange(OrganizationEntity org, string username, string password)
        {
            var outlookClient = new HttpClient();
            outlookClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenFor(org, username, password, OutlookResource));
            return new ExchangeRestWrapper(outlookClient);
        }

        public async Task<GraphRestWrapper> CreateGraph(OrganizationEntity org, string username, string password)
        {
            var graphClient = new HttpClient();
            graphClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenFor(org, username, password, GraphResource));
            return new GraphRestWrapper(graphClient);
        }

        private async Task<string> GetAccessTokenFor(OrganizationEntity org, string username, string password, string resource)
        {
            var vals = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("scope", "openid"),
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            };

            using (HttpClient hc = new HttpClient())
            {
                var content = new FormUrlEncodedContent(vals);
                var hrm = await hc.PostAsync(Authority, content).ConfigureAwait(false);
                hrm.EnsureSuccessStatusCode();
                var response = JObject.Parse(await hrm.Content.ReadAsStringAsync()).ToObject<LoginResponse>();

                return response.access_token;
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
