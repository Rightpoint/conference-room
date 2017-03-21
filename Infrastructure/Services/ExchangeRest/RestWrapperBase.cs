using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public abstract class RestWrapperBase
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly HttpClient _client;
        private readonly HttpClient _longCallClient;
        protected abstract Uri BaseUri { get; }

        protected RestWrapperBase(HttpClient client, HttpClient longCallClient)
        {
            _client = client;
            _longCallClient = longCallClient;
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
            var uri = new Uri(BaseUri, url).AbsoluteUri;
            log.DebugFormat("Starting request for {0}", uri);
            var req = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
            using (var r = await _longCallClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                r.EnsureSuccessStatusCode();
                using (var s = await r.Content.ReadAsStreamAsync())
                {
                    using (var tr = new StreamReader(s))
                    {
                        using (var jr = new JsonTextReader(tr))
                        {
                            log.DebugFormat("Starting reading response for {0}", uri);
                            cancellationToken.Register(() =>
                            {
                                log.DebugFormat("Got cancellation request");
                                jr.Close();
                                tr.Close();
                                s.Close();
                            });
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.Run(() =>
                            {
                                while (jr.TokenType != JsonToken.StartArray)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    log.DebugFormat("Pre-consuming {0}", jr.TokenType);
                                    jr.Read();
                                }
                                cancellationToken.ThrowIfCancellationRequested();
                                log.DebugFormat("Consuming {0}, CT: {1}", jr.TokenType, cancellationToken);
                                jr.Read();
                                while (jr.TokenType != JsonToken.EndArray)
                                {
                                    log.DebugFormat("Processing {0}", jr.TokenType);
                                    callback(JObject.Load(jr));
                                    cancellationToken.ThrowIfCancellationRequested();
                                    log.DebugFormat("Processing complete with {0}, consuming", jr.TokenType);
                                    jr.Read();
                                }
                            }, cancellationToken);
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
