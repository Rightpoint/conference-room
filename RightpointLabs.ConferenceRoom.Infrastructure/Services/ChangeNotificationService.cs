using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Services;
using Timer = System.Timers.Timer;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ChangeNotificationService : IChangeNotificationService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ExchangeService _exchangeService;
        private readonly IBroadcastService _broadcastService;
        private readonly IMeetingCacheService _meetingCacheService;

        private Timer _startConnectionTimer = new Timer(10000);
        public ChangeNotificationService(ExchangeService exchangeService, IBroadcastService broadcastService, IMeetingCacheService meetingCacheService)
        {
            _exchangeService = exchangeService;
            _broadcastService = broadcastService;
            _meetingCacheService = meetingCacheService;
            _startConnectionTimer.Stop();
            _startConnectionTimer.Elapsed += StartConnectionTimerOnElapsed;
        }

        private void StartConnectionTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            UpdateConnection();
            _startConnectionTimer.Stop();
        }

        private StreamingSubscriptionConnection StartNewConnection()
        {
            var subIdToRoomAddress = new Dictionary<string, string>();
            var subs = new List<StreamingSubscription>();
            foreach (var room in _roomsTracked)
            {
                var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(room));
                var sub = _exchangeService.SubscribeToStreamingNotifications(
                    new [] { calId },
                    EventType.Created,
                    EventType.Deleted,
                    EventType.Modified,
                    EventType.Moved,
                    EventType.Copied,
                    EventType.FreeBusyChanged);
                subIdToRoomAddress.Add(sub.Id, room);
                subs.Add(sub);
            }

            // Create a streaming connection to the service object, over which events are returned to the client.
            // Keep the streaming connection open for 30 minutes.
            var connection = new StreamingSubscriptionConnection(_exchangeService, 30);
            subs.ForEach(connection.AddSubscription);
            connection.OnNotificationEvent += OnNotificationEvent;
            connection.OnDisconnect += OnDisconnect;
            connection.Open();
            _subIdToRoomAddress = subIdToRoomAddress;
            _meetingCacheService.ClearAll();
            log.DebugFormat("Opened subscription to {0}", string.Join(", ", subIdToRoomAddress.Values));
            return connection;
        }

        private void OnDisconnect(object sender, SubscriptionErrorEventArgs args)
        {
            _meetingCacheService.ClearAll();
            UpdateConnection();
        }

        private void OnNotificationEvent(object sender, NotificationEventArgs args)
        {
            log.DebugFormat("Got args: {0}", args.Subscription.Id);
            var d = _subIdToRoomAddress;
            if (null != d)
            {
                var room = d.TryGetValue(args.Subscription.Id);
                if (!string.IsNullOrEmpty(room))
                {
                    log.DebugFormat("Got update for room: {0} - {1}", args.Subscription.Id, room);
                    _meetingCacheService.ClearUpcomingAppointmentsForRoom(room);
                    _broadcastService.BroadcastUpdate(room);
                }
            }
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

        private Dictionary<string, string> _subIdToRoomAddress = new Dictionary<string, string>();
        private HashSet<string> _roomsTracked = new HashSet<string>();
        private StreamingSubscriptionConnection _connection;

        public void TrackRoom(string roomAddress)
        {
            if (_roomsTracked.Contains(roomAddress)) 
                return;
            _roomsTracked.Add(roomAddress);
            UpdateConnection();
        }
    }
}