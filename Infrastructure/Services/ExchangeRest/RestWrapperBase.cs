using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public abstract class RestWrapperBase
    {
        private readonly HttpClient _client;
        protected abstract Uri BaseUri { get; }

        protected RestWrapperBase(HttpClient client)
        {
            _client = client;
        }

        protected async Task<T> GetRaw<T>(string url) where T : JToken
        {
            using (var s = await _client.GetStreamAsync(new Uri(BaseUri, url).AbsoluteUri))
            {
                using (var tr = new StreamReader(s))
                {
                    using (var jr = new JsonTextReader(tr))
                    {
                        return (T)JToken.Load(jr);
                    }
                }
            }
        }

        protected async Task<T> Get<T>(string url)
        {
            return (await GetRaw<JToken>(url)).ToObject<T>();
        }
    }
}
