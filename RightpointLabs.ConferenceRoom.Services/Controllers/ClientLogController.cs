using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with client log messages
    /// </summary>
    [RoutePrefix("api/clientLog")]
    public class ClientLogController : ApiController
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public class ClientMessagesMessage
        {
            public ClientMessage[] Messages;
        }

        public class ClientMessage
        {
            public int Count;
            public DateTime Time;
            public string Level;
            public string Message;
        }

        [Route("messages")]
        public void PostMessages([FromBody]ClientMessagesMessage message)
        {
            var ip = GetClientIp(Request);
            foreach (var msg in message.Messages)
            {
                switch (msg.Level)
                {
                    case "log":
                    case "debug":
                        log.DebugFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "info":
                        log.InfoFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "warn":
                        log.WarnFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "error":
                        log.ErrorFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    default:
                        log.DebugFormat("[{4}] Time: {0}, Level: {1}, Message: ({2}x) {3}", msg.Time, msg.Level, msg.Count, msg.Message, ip);
                        break;
                }
            }
        }


        private static string GetClientIp(HttpRequestMessage request)
        {
            // from https://trikks.wordpress.com/2013/06/27/getting-the-client-ip-via-asp-net-web-api/
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
    }
}
