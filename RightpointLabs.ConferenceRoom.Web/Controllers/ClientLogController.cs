using log4net;
using System;
using System.Reflection;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with client log messages
    /// </summary>
    [RoutePrefix("api/clientLog")]
    public class ClientLogController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public ClientLogController()
            : base(__log)
        { }

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
            var ip = GetClientIp(this.Request);
            foreach (var msg in message.Messages)
            {
                switch (msg.Level)
                {
                    case "log":
                    case "debug":
                        this.Log.DebugFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "info":
                        this.Log.InfoFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "warn":
                        this.Log.WarnFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    case "error":
                        this.Log.ErrorFormat("[{3}] Time: {0}, Message: ({1}x) {2}", msg.Time, msg.Count, msg.Message, ip);
                        break;
                    default:
                        this.Log.DebugFormat("[{4}] Time: {0}, Level: {1}, Message: ({2}x) {3}", msg.Time, msg.Level, msg.Count, msg.Message, ip);
                        break;
                }
            }
        }
    }
}
