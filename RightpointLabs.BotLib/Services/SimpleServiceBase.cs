using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.BotLib.Services
{
    public abstract class SimpleServiceBase : IDisposable
    {
        protected abstract Uri Url { get; }
        private HttpClient _client = null;

        protected async Task<T> GetRaw<T>(string url) where T : JToken
        {
            //var key = GetUserKey() + "_" + url;
            //return await RedisCache.Instance.GetSetAsync(key, TimeSpan.FromMinutes(10), () => _GetRaw<T>(url));
            return await _GetRaw<T>(url);
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

            using (var s = await _client.GetStreamAsync(new Uri(Url, url).AbsoluteUri))
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
    }
}