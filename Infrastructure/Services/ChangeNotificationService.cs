using System;
using System.Collections.Generic;
using System.Configuration;
using System.Reflection;
using System.Timers;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Services;
using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ChangeNotificationService : IChangeNotificationService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBroadcastService _broadcastService;
        private readonly IMeetingCacheService _meetingCacheService;

        public ChangeNotificationService(IBroadcastService broadcastService, IMeetingCacheService meetingCacheService)
        {
            _broadcastService = broadcastService;
            _meetingCacheService = meetingCacheService;
        }

        private Dictionary<string, Watcher> _roomsTracked = new Dictionary<string, Watcher>();

        public void TrackRoom(IRoom room, IExchangeServiceManager exchangeServiceManager, OrganizationEntity organization)
        {
            lock (_roomsTracked)
            {
                if (_roomsTracked.ContainsKey(room.RoomAddress))
                    return;
                _roomsTracked.Add(room.RoomAddress, new Watcher(exchangeServiceManager, _broadcastService, _meetingCacheService, room, organization));
            }
        }

        public bool IsTrackedForChanges(IRoom room)
        {
            return _roomsTracked.TryGetValue(room.RoomAddress).ChainIfNotNull(i => i.IsActive);
        }

        private class Watcher
        {
            private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            private readonly IExchangeServiceManager _exchangeServiceManager;
            private readonly IBroadcastService _broadcastService;
            private readonly IMeetingCacheService _meetingCacheService;
            private readonly IRoom _room;
            private readonly OrganizationEntity _organization;

            private StreamingSubscriptionConnection _connection;
            private Timer _startConnectionTimer = new Timer(10000);

            public bool IsActive { get; private set; }

            public Watcher(IExchangeServiceManager exchangeServiceManager, IBroadcastService broadcastService, IMeetingCacheService meetingCacheService, IRoom room, OrganizationEntity organization)
            {
                IsActive = false;

                _exchangeServiceManager = exchangeServiceManager;
                _broadcastService = broadcastService;
                _meetingCacheService = meetingCacheService;
                _room = room;
                _organization = organization;
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
                return _exchangeServiceManager.ExecutePrivate(_room.RoomAddress, svc =>
                {
                    log.DebugFormat("Opening subscription to {0}", _room.RoomAddress);
                    var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(_room.RoomAddress));
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
                    _meetingCacheService.ClearUpcomingAppointmentsForRoom(_room.RoomAddress);
                    log.DebugFormat("Opened subscription to {0} via {1} with {2}", _room.RoomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
                    IsActive = true;
                    return connection;
                });
            }

            private void OnDisconnect(object sender, SubscriptionErrorEventArgs args)
            {
                _meetingCacheService.ClearUpcomingAppointmentsForRoom(_room.RoomAddress);
                IsActive = false;
                UpdateConnection();
            }

            private void OnNotificationEvent(object sender, NotificationEventArgs args)
            {
                _meetingCacheService.ClearUpcomingAppointmentsForRoom(_room.RoomAddress);
                _broadcastService.BroadcastUpdate(_organization, _room);
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