using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class GraphRestWrapper : RestWrapperBase
    {
        protected override Uri BaseUri { get; } = new Uri("https://graph.microsoft.com/");

        public GraphRestWrapper(HttpClient client) : base(client)
        {
        }

        public async Task<string> GetUserDisplayName(string roomAddress)
        {
            return (await Get<UserResult>($"v1.0/users/{roomAddress}"))?.DisplayName;
        }

        private class UserResult
        {
            public string DisplayName { get; set; }
            public string Mail { get; set; }
        }
    }
}
