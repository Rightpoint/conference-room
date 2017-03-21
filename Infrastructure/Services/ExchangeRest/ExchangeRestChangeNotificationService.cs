using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using log4net;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestChangeNotificationService : IExchangeRestChangeNotificationService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IBroadcastService _broadcastService;
        private readonly IMeetingCacheService _meetingCacheService;
        private readonly IIOCContainer _container;
        private readonly ConcurrentDictionary<string, Watcher> _orgsTracked = new ConcurrentDictionary<string, Watcher>();

        public ExchangeRestChangeNotificationService(IBroadcastService broadcastService, IMeetingCacheService meetingCacheService, IIOCContainer container)
        {
            if (broadcastService == null) throw new ArgumentNullException(nameof(broadcastService));
            if (meetingCacheService == null) throw new ArgumentNullException(nameof(meetingCacheService));
            if (container == null) throw new ArgumentNullException(nameof(container));
            _broadcastService = broadcastService;
            _meetingCacheService = meetingCacheService;
            _container = container;
        }

        public void TrackOrganization(string organizationId)
        {
            _orgsTracked.GetOrAdd(organizationId, _ => new Watcher(_container, organizationId, _broadcastService, _meetingCacheService));
        }

        public void UpdateRooms(string organizationId)
        {
            _orgsTracked.GetOrAdd(organizationId, _ => new Watcher(_container, organizationId, _broadcastService, _meetingCacheService)).UpdateRooms();
        }

        public bool IsTrackedForChanges(string organizationId)
        {
            return _orgsTracked.TryGetValue(organizationId) != null;
        }

        private class Watcher
        {
            private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            private readonly IIOCContainer _container;
            private readonly IBroadcastService _broadcastService;
            private readonly IMeetingCacheService _meetingCacheService;
            private readonly string _organizationId;
            private volatile CancellationTokenSource _cancellationTokenSource;

            public Watcher(IIOCContainer container, string organizationId, IBroadcastService broadcastService, IMeetingCacheService meetingCacheService)
            {
                _container = container;
                _organizationId = organizationId;
                _broadcastService = broadcastService;
                _meetingCacheService = meetingCacheService;

                Task.Run(async () => await DoWork()); // don't wait for this to complete
            }

            private class CustomOrganizationContextService : IContextService
            {
                private readonly OrganizationEntity _organization;

                public CustomOrganizationContextService(OrganizationEntity organization)
                {
                    _organization = organization;
                }

                public bool IsAuthenticated => true;
                public string UserId => null;
                public DeviceEntity CurrentDevice => null;
                public OrganizationEntity CurrentOrganization => _organization;
            }

            private async Task DoWork()
            {
                while (true)
                {
                    log.DebugFormat("Starting RestChangeNotification loop: {0}", _container);
                    try
                    {
                        _cancellationTokenSource = new CancellationTokenSource();
                        var token = _cancellationTokenSource.Token;
                        using (var cn = _container.CreateChildContainer())
                        {
                            var orgTask = cn.Resolve<IOrganizationRepository>().GetAsync(_organizationId);
                            var roomTask = cn.Resolve<IRoomMetadataRepository>().GetRoomInfosForOrganizationAsync(_organizationId);

                            var org = await orgTask;
                            var rooms = (await roomTask).ToList();

                            cn.RegisterInstance((IContextService)new CustomOrganizationContextService(org));
                            rooms.ForEach(r => _meetingCacheService.ClearUpcomingAppointmentsForRoom(r.RoomAddress));
                            var exch = cn.Resolve<ExchangeRestWrapper>();
                            log.DebugFormat("Creating {0} tasks", rooms.Count);
                            var subTasks = rooms.Select(r => new {Room = r, Task = exch.CreateNotification(r.RoomAddress)}).ToList();
                            await Task.WhenAll(subTasks.Select(_ => _.Task));
                            log.DebugFormat("Completed {0} tasks", rooms.Count);
                            var subscriptions = subTasks.Where(i => i.Task.IsCompleted).ToDictionary(i => i.Task.Result.Id, i => i.Room);
                            log.DebugFormat("Created {0} subscriptions", subscriptions.Count);
                            foreach(var room in subscriptions.Values) {
                                _meetingCacheService.ClearUpcomingAppointmentsForRoom(room.RoomAddress);
                            }

                            try
                            {
                                var tasks = new List<Task>();
                                foreach (var keys in subscriptions.Keys.Chunk(20))
                                {
                                    log.DebugFormat("Waiting for notifications from {0} subscriptions", keys.Length);
                                    // stream subscriptions
                                    var req = new NotificationRequest()
                                    {
                                        ConnectionTimeoutInMinutes = 120,
                                        KeepAliveNotificationIntervalInSeconds = 30,
                                        SubscriptionIds = keys,
                                    };
                                    tasks.Add(Task.Run(async () =>
                                    {
                                        await exch.GetNotifications(req, resp =>
                                        {
                                            var room = subscriptions.TryGetValue(resp.SubscriptionId);
                                            log.DebugFormat("Processing {0} for {1} on {2}", resp.ChangeType, room.RoomAddress, resp.SubscriptionId);
                                            _meetingCacheService.ClearUpcomingAppointmentsForRoom(room.RoomAddress);
                                            _broadcastService.BroadcastUpdate(org, room);
                                        }, _cancellationTokenSource.Token);
                                    }));
                                }
                                try
                                {
                                    await Task.WhenAny(tasks);
                                }
                                catch (Exception ex)
                                {
                                    log.DebugFormat("WhenAny got error: {0}", ex.Message);
                                }
                                log.DebugFormat("Cancelling outstanding work");
                                _cancellationTokenSource.Cancel();
                                await Task.WhenAll(tasks);
                            }
                            catch (Exception ex)
                            {
                                log.DebugFormat("DoWork got error: {0}", ex.Message);
                            }
                            _cancellationTokenSource.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.DebugFormat("Issue from main change-detect loop: {0}", ex);
                        await Task.Delay(1000);
                    }
                }
            }
            
            public void UpdateRooms()
            {
                _cancellationTokenSource?.Cancel();
            }
        }
    }
}