using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Timers;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Services;
using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ChangeNotificationService : IChangeNotificationService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Func<ExchangeService> _exchangeServiceBuilder;
        private readonly IBroadcastService _broadcastService;
        private readonly IMeetingCacheService _meetingCacheService;

        public ChangeNotificationService(Func<ExchangeService> exchangeServiceBuilder, IBroadcastService broadcastService, IMeetingCacheService meetingCacheService)
        {
            _exchangeServiceBuilder = exchangeServiceBuilder;
            _broadcastService = broadcastService;
            _meetingCacheService = meetingCacheService;
        }

        private Dictionary<string, Watcher> _roomsTracked = new Dictionary<string, Watcher>();

        public void TrackRoom(string roomAddress)
        {
            lock (_roomsTracked)
            {
                if (_roomsTracked.ContainsKey(roomAddress))
                    return;
                _roomsTracked.Add(roomAddress, new Watcher(_exchangeServiceBuilder, _broadcastService, _meetingCacheService, roomAddress));
            }
        }

        public bool IsTrackedForChanges(string roomAddress)
        {
            return _roomsTracked.TryGetValue(roomAddress).ChainIfNotNull(i => i.IsActive);
        }

        private class Watcher
        {
            private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            private readonly Func<ExchangeService> _exchangeServiceBuilder;
            private readonly IBroadcastService _broadcastService;
            private readonly IMeetingCacheService _meetingCacheService;
            private readonly string _roomAddress;

            private StreamingSubscriptionConnection _connection;
            private Timer _startConnectionTimer = new Timer(10000);

            public bool IsActive { get; private set; }

            public Watcher(Func<ExchangeService> exchangeServiceBuilder, IBroadcastService broadcastService, IMeetingCacheService meetingCacheService, string roomAddress)
            {
                IsActive = false;

                _exchangeServiceBuilder = exchangeServiceBuilder;
                _broadcastService = broadcastService;
                _meetingCacheService = meetingCacheService;
                _roomAddress = roomAddress;
                _startConnectionTimer.Stop();
                _startConnectionTimer.Elapsed += StartConnectionTimerOnElapsed;
                Task.Run(() => UpdateConnection()); // don't wait for this to complete
            }

            private void StartConnectionTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
            {
                _startConnectionTimer.Stop();
                UpdateConnection();
            }

            private StreamingSubscriptionConnection StartNewConnection()
            {
                var svc = _exchangeServiceBuilder();
                var useImpersonation = bool.Parse(ConfigurationManager.AppSettings["useImpersonation"] ?? "false");
                if (useImpersonation)
                {
                    svc.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, _roomAddress);
                }
                log.DebugFormat("Opening subscription to {0}, impersonation: {1}", _roomAddress, useImpersonation);
                var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(_roomAddress));
                var sub = svc.SubscribeToStreamingNotifications(
                    new[] { calId },
                    EventType.Created,
                    EventType.Deleted,
                    EventType.Modified,
                    EventType.Moved,
                    EventType.Copied,
                    EventType.FreeBusyChanged);

                // Create a streaming connection to the service object, over which events are returned to the client.
                // Keep the streaming connection open for 30 minutes.
                var connection = new StreamingSubscriptionConnection(svc, 30);
                connection.AddSubscription(sub);
                connection.OnNotificationEvent += OnNotificationEvent;
                connection.OnDisconnect += OnDisconnect;
                connection.Open();
                _meetingCacheService.ClearUpcomingAppointmentsForRoom(_roomAddress);
                log.DebugFormat("Opened subscription to {0} via {1} with {2}", _roomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
                IsActive = true;
                return connection;
            }

            private void OnDisconnect(object sender, SubscriptionErrorEventArgs args)
            {
                _meetingCacheService.ClearUpcomingAppointmentsForRoom(_roomAddress);
                IsActive = false;
                UpdateConnection();
            }

            private void OnNotificationEvent(object sender, NotificationEventArgs args)
            {
                _meetingCacheService.ClearUpcomingAppointmentsForRoom(_roomAddress);
                _broadcastService.BroadcastUpdate(_roomAddress);
            }

            private object _connectionLock = new object();
            private void UpdateConnection()
            {
                try
                {
                    lock (_connectionLock)
                    {
                        if (null != _connection)
                        {
                            if (_connection.IsOpen)
                            {
                                _connection.Close();
                            }
                        }
                        _connection = StartNewConnection();
                    }
                }
                catch (Exception ex)
                {
                    log.WarnFormat("Error setting up connection: {0}", ex);
                    _startConnectionTimer.Start(); // retry
                }
            }
        }
    }
}