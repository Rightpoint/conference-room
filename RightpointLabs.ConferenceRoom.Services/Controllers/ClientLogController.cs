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
            foreach (var msg in message.Messages)
            {
                switch (msg.Level)
                {
                    case "log":
                    case "debug":
                        log.DebugFormat("Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message);
                        break;
                    case "info":
                        log.InfoFormat("Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message);
                        break;
                    case "warn":
                        log.WarnFormat("Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message);
                        break;
                    case "error":
                        log.ErrorFormat("Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message);
                        break;
                    default:
                        log.DebugFormat("Time: {0}, Level: {1}, Message: ({2}x) {3}", msg.Time, msg.Level, msg.Count, msg.Message);
                        break;
                }
            }
        }
    }
}
