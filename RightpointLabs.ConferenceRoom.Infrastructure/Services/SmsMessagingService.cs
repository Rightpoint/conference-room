using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Plivo.API;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class SmsMessagingService : ISmsMessagingService
    {
        private readonly string _authId;
        private readonly string _authToken;
        private readonly string _fromNumber;

        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SmsMessagingService(string authId, string authToken, string fromNumber)
        {
            _authId = authId;
            _authToken = authToken;
            _fromNumber = fromNumber;
        }

        public void Send(string[] numbers, string message)
        {
            var api = new RestAPI(_authId, _authToken);
            var resp = api.send_message(new Dictionary<string, string>()
            {
                {"src", _fromNumber},
                {"dst", string.Join("<", numbers) },
                {"text", message}
            });
            if (resp.ErrorException != null)
            {
                throw new Exception("Error sending SMS", resp.ErrorException);
            }
            if (!string.IsNullOrEmpty(resp.ErrorMessage))
            {
                throw new Exception("Error sending SMS: " + resp.ErrorMessage);
            }
            if (null == resp.Data)
            {
                throw new Exception("Error sending SMS: no message response recieved");
            }
            log.DebugFormat("{0} - {1}: {2}", resp.Data.api_id, resp.Data.message, string.Join(", ", resp.Data.message_uuid));
        }
    }
}