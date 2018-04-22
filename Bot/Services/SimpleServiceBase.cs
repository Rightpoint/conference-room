using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Bot.Controllers;

namespace RightpointLabs.ConferenceRoom.Bot.Services
{
    public abstract class SimpleServiceBase : IDisposable
    {
        protected abstract Uri Url { get; }
        private HttpClient _client = null;

        protected async Task<T> GetRaw<T>(string url) where T : JToken
        {
            return await _GetRaw<T>(url);
            //var key = GetUserKey() + "_" + url;
            //return await RedisCache.Instance.GetSetAsync(key, TimeSpan.FromMinutes(10), () => _GetRaw<T>(url));
        }

        protected async Task<T> _GetRaw<T>(string url) where T : JToken
        {
            if (null == _client)
            {
                var h = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    CookieContainer = await GetCookieContainer(),
                    Credentials = await GetCredentials()
                };
                var c = new HttpClient(h);
                AddAuthentication(c);
                if (null != Interlocked.CompareExchange(ref _client, c, null))
                {
                    c.Dispose();
                    h.Dispose();
                }
            }

            var sw = Stopwatch.StartNew();
            using (var res = await _client.GetAsync(new Uri(Url, url).AbsoluteUri))
            {
                RequestComplete(res, sw.Elapsed);
                res.EnsureSuccessStatusCode();
                using (var tr = new StreamReader(await res.Content.ReadAsStreamAsync()))
                {
                    using (var jr = new JsonTextReader(tr))
                    {
                        return (T)JToken.Load(jr);
                    }
                }
            }
        }

        protected async Task<string> Post(string url, HttpContent content)
        {
            if (null == _client)
            {
                var h = new HttpClientHandler()
                {
                    AllowAutoRedirect = false,
                    CookieContainer = await GetCookieContainer(),
                    Credentials = await GetCredentials()
                };
                var c = new HttpClient(h);
                AddAuthentication(c);
                if (null != Interlocked.CompareExchange(ref _client, c, null))
                {
                    c.Dispose();
                    h.Dispose();
                }
            }

            var sw = Stopwatch.StartNew();
            using (var r = await _client.PostAsync(new Uri(Url, url).AbsoluteUri, content))
            {
                RequestComplete(r, sw.Elapsed);
                var responseString = await r.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseString))
                {
                    responseString = r.StatusCode.ToString();
                }
                if (!r.IsSuccessStatusCode)
                {
                    throw new ApplicationException(responseString);
                }
                r.EnsureSuccessStatusCode();
                return responseString;
            }
        }

        protected virtual async Task<ICredentials> GetCredentials()
        {
            return null;
        }

        protected virtual async Task<CookieContainer> GetCookieContainer()
        {
            return new CookieContainer();
        }

        protected virtual void AddAuthentication(HttpClient c)
        {
            c.DefaultRequestHeaders.Add("x-ms-request-id", MessagesController.TelemetryClient.Context.Operation.Id);
        }

        protected virtual string GetUserKey()
        {
            return "";
        }

        protected async Task<T> Get<T>(string url)
        {
            return (await GetRaw<JToken>(url)).ToObject<T>();
        }

        public void Dispose()
        {
            if (null != _client)
            {
                _client.Dispose();
                _client = null;
            }
        }
        private void RequestComplete(HttpResponseMessage res, TimeSpan duration)
        {
            var now = DateTimeOffset.Now;
            MessagesController.TelemetryClient.TrackDependency(
                "Http",
                $"{res.RequestMessage.RequestUri.Host}:{res.RequestMessage.RequestUri.Port}",
                $"{res.RequestMessage.Method} {res.RequestMessage.RequestUri.AbsolutePath}",
                res.RequestMessage.RequestUri.ToString(), 
                now.Subtract(duration), 
                duration, 
                $"{(int)res.StatusCode}",
                res.IsSuccessStatusCode);
        }
    }
}