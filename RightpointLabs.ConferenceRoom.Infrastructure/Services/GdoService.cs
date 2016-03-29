using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class GdoService : IGdoService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly string _apiKey;
        private readonly string _username;
        private readonly string _password;
        private readonly Uri _baseUri;
        private string _token = null;

        public GdoService(Uri baseUri, string apiKey, string username, string password)
        {
            _baseUri = baseUri;
            _apiKey = apiKey;
            _username = username;
            _password = password;
        }

        private class ApiResponse
        {
            public string ReturnCode { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class TokenResponse : ApiResponse
        {
            public string SecurityToken { get; set; }
        }

        private class DevicesResponse : ApiResponse
        {
            public Device[] Devices { get; set; }
        }

        private class Device
        {
            public string MyQDeviceId { get; set; }
            public Attr[] Attributes { get; set; }
        }

        private class Attr
        {
            public string AttributeDisplayName { get; set; }
            public string Value { get; set; }
            public DateTime? UpdatedDate { get; set; }
        }

        private async Task<string> GetToken()
        {
            using (var c = new HttpClient())
            {
                var uri = new Uri(_baseUri, "user/validate?" + string.Join("&", new Dictionary<string, string>()
                {
                    {"username", _username},
                    {"password", _password},
                    {"appId", _apiKey},
                }.Select(i => string.Format("{0}={1}", i.Key, i.Value))));
                var res = await c.GetAsync(uri);
                res.EnsureSuccessStatusCode();
                var obj = JsonConvert.DeserializeObject<TokenResponse>(await res.Content.ReadAsStringAsync());
                if (obj.ReturnCode != "0")
                {
                    throw new ArgumentException(obj.ErrorMessage);
                }
                return obj.SecurityToken;
            }
        }

        private async Task<T> CallWithToken<T>(Func<string, Task<T>> action)
        {
            if (null != _token)
            {
                try
                {
                    return await action(_token);
                }
                catch (Exception ex)
                {
                    // ignore - try again with token
                }
            }
            _token = await GetToken();
            return await action(_token);
        }

        public async Task<string> GetStatus(string deviceId)
        {
            return await CallWithToken(async token =>
            {
                using (var c = new HttpClient())
                {
                    var uri = new Uri(_baseUri, "v4/userdevicedetails/get?" + string.Join("&", new Dictionary<string, string>()
                    {
                        {"appId", _apiKey},
                        {"SecurityToken", token },
                    }.Select(i => string.Format("{0}={1}", i.Key, i.Value))));
                    var res = await c.GetAsync(uri);
                    res.EnsureSuccessStatusCode();
                    var obj = JsonConvert.DeserializeObject<DevicesResponse>(await res.Content.ReadAsStringAsync());
                    if (obj.ReturnCode != "0")
                    {
                        throw new ArgumentException(obj.ErrorMessage);
                    }
                    var status = obj.Devices.Where(i => i.MyQDeviceId == deviceId).SelectMany(i => i.Attributes).Where(i => i.AttributeDisplayName == "doorstate").Select(i => i.Value).FirstOrDefault();
                    log.DebugFormat("Got status: {0}", status);
                    return status == "2" ? "Closed" : "Open"; // TODO: add more statuses here
                }
            });
        }

        public Task Open(string deviceId)
        {
            return SetDoorState(deviceId, 1);
        }

        public Task Close(string deviceId)
        {
            return SetDoorState(deviceId, 0);
        }

        private async Task SetDoorState(string deviceId, int state)
        {
            await CallWithToken(async token =>
            {
                using (var c = new HttpClient())
                {
                    var uri = new Uri(_baseUri, "v4/deviceattribute/putdeviceattribute?" + string.Join("&", new Dictionary<string, string>()
                    {
                        {"appId", _apiKey},
                        {"SecurityToken", token },
                    }.Select(i => string.Format("{0}={1}", i.Key, i.Value))));
                    var callObj = new
                    {
                        MyQDeviceId = deviceId,
                        AttributeName = "desireddoorstate",
                        AttributeValue = state
                    };
                    var content = new StringContent(JsonConvert.SerializeObject(callObj));
                    content.Headers.ContentType.MediaType = "application/json";
                    content.Headers.ContentType.CharSet = "utf-8";
                    var res = await c.PutAsync(uri, content);
                    res.EnsureSuccessStatusCode();
                    var obj = JsonConvert.DeserializeObject<ApiResponse>(await res.Content.ReadAsStringAsync());
                    if (obj.ReturnCode != "0")
                    {
                        throw new ArgumentException(obj.ErrorMessage);
                    }
                    return obj;
                }
            });
        }
    }
}
