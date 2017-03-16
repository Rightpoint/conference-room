using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
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

        protected async Task Post(string url, HttpContent content)
        {
            using (var r = await _client.PostAsync(new Uri(BaseUri, url).AbsoluteUri, content))
            {
                r.EnsureSuccessStatusCode();
            }
        }
        protected async Task<T> Post<T>(string url, HttpContent content)
        {
            using (var r = await _client.PostAsync(new Uri(BaseUri, url).AbsoluteUri, content))
            {
                r.EnsureSuccessStatusCode();
                using (var s = await r.Content.ReadAsStreamAsync())
                {
                    using (var tr = new StreamReader(s))
                    {
                        using (var jr = new JsonTextReader(tr))
                        {
                            return JToken.Load(jr).ToObject<T>();
                        }
                    }
                }
            }
        }

        protected async Task Patch(string url, HttpContent content)
        {
            using (var r = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, new Uri(BaseUri, url).AbsoluteUri)
            {
                Content = content
            }))
            {
                r.EnsureSuccessStatusCode();
            }
        }


        protected async Task PostStreamResponse(string url, HttpContent content, Action<JObject> callback, CancellationToken cancellationToken)
        {
            using (var r = await _client.PostAsync(new Uri(BaseUri, url).AbsoluteUri, content, cancellationToken))
            {
                r.EnsureSuccessStatusCode();
                using (var s = await r.Content.ReadAsStreamAsync())
                {
                    using (var tr = new StreamReader(s))
                    {
                        using (var jr = new JsonTextReader(tr))
                        {
                            await Task.Run(() =>
                            {
                                while (jr.TokenType != JsonToken.StartArray)
                                {
                                    jr.Read();
                                }
                                jr.Read();
                                while (jr.TokenType != JsonToken.EndArray)
                                {
                                    callback(JObject.Load(jr));
                                }
                            });

                        }
                    }
                }
            }
        }

        public class Response<T>
        {
            public T Value { get; set; }
        }
    }
}
