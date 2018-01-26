using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Plivo;
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
            var api = new PlivoApi(_authId, _authToken);
            var resp = api.Message.Create(_fromNumber, numbers.ToList(), message);
            log.DebugFormat("{0} - {1}: {2}", resp.ApiId, resp.Message, string.Join(", ", resp.MessageUuid));
        }
    }
}