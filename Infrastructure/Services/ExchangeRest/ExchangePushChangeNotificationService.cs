using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangePushChangeNotificationService : IExchangeRestChangeNotificationService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBroadcastService _broadcastService;
        private readonly IMeetingCacheService _meetingCacheService;
        private readonly IIOCContainer _container;
        private readonly string _connectionString;
        private readonly string _topic;
        private readonly string _subscription;

        public ExchangePushChangeNotificationService(IBroadcastService broadcastService, IMeetingCacheService meetingCacheService, IIOCContainer container, string connectionString, string topic, string subscription)
        {
            if (broadcastService == null) throw new ArgumentNullException(nameof(broadcastService));
            if (meetingCacheService == null) throw new ArgumentNullException(nameof(meetingCacheService));
            if (container == null) throw new ArgumentNullException(nameof(container));
            _broadcastService = broadcastService;
            _meetingCacheService = meetingCacheService;
            _container = container;
            _connectionString = connectionString;
            _topic = topic;
            _subscription = subscription;
        }

        public void RecieveMessages()
        {
            while (true)
            {
                log.DebugFormat("Starting push change notification listener");
                try
                {
                    var namespaceManager = NamespaceManager.CreateFromConnectionString(_connectionString);
                    if (!namespaceManager.TopicExists(_topic))
                    {
                        namespaceManager.CreateTopic(_topic);
                    }

                    if (!namespaceManager.SubscriptionExists(_topic, _subscription))
                    {
                        namespaceManager.CreateSubscription(_topic, _subscription);
                    }

                    var messagingFactory = MessagingFactory.CreateFromConnectionString(_connectionString);
                    messagingFactory.GetSettings().OperationTimeout = TimeSpan.FromHours(1);

                    var subscriptionClient = messagingFactory.CreateSubscriptionClient(_topic, _subscription, ReceiveMode.PeekLock);

                    Action<BrokeredMessage> handleMessage = ProcessMessage;

                    subscriptionClient.OnMessage(handleMessage, new OnMessageOptions
                    {
                        AutoComplete = false,
                        MaxConcurrentCalls = 10,
                        AutoRenewTimeout = TimeSpan.FromMinutes(1)
                    });

                    log.Debug("Push change notification listener started");
                    break;
                }
                catch (Exception ex)
                {
                    log.ErrorFormat("Unexpected exception initializing service bus connection - waiting 15 seconds and retrying: {0}", ex);
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
            }
        }

        private void ProcessMessage(BrokeredMessage message)
        {
            try
            {
                using (var s = message.GetBody<Stream>())
                {
                    using (var tr = new StreamReader(s))
                    {
                        using (var jr = new JsonTextReader(tr))
                        {
                            var obj = JObject.ReadFrom(jr);
                            var msg = obj.ToObject<Message>();
                            var room = msg.Room;
                            log.Debug($"Got notification for {room.RoomAddress} as {room.Id} @ {room.OrganizationId}");
                            _meetingCacheService.ClearUpcomingAppointmentsForRoom(room.Id);
                            _broadcastService.BroadcastUpdate(new OrganizationEntity() { Id = room.OrganizationId }, room);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn($"Failed to handle incoming message: {ex}");
            }
        }

        private class Message
        {
            public JObject Notification { get; set; }
            public RoomMetadataEntity Room { get; set; }
        }

        public void TrackOrganization(string organizationId)
        {
        }

        public void UpdateRooms(string organizationId)
        {
        }

        public bool IsTrackedForChanges(string organizationId)
        {
            return true;
        }
    }
}