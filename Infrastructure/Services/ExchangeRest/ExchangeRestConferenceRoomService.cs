using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest
{
    public class ExchangeRestConferenceRoomService : IConferenceRoomService
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly bool _ignoreFree = true;
        private readonly string[] _emailDomains = new[] {"rightpoint.com"};

        private readonly ExchangeRestWrapper _exchange;
        private readonly GraphRestWrapper _graph;
        private readonly IContextService _contextService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IFloorRepository _floorRepository;
        private readonly IMeetingRepository _meetingRepository;
        private readonly IBroadcastService _broadcastService;
        private readonly ISignatureService _signatureService;
        private readonly ISmsAddressLookupService _smsAddressLookupService;
        private readonly ISmsMessagingService _smsMessagingService;
        private readonly IInstantMessagingService _instantMessagingService;
        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IMeetingCacheService _meetingCacheService;
        private readonly IExchangeRestChangeNotificationService _exchangeRestChangeNotificationService;

        public ExchangeRestConferenceRoomService(ExchangeRestWrapper exchange, GraphRestWrapper graph, IContextService contextService, IDateTimeService dateTimeService, IBuildingRepository buildingRepository, IFloorRepository floorRepository, IMeetingRepository meetingRepository, IBroadcastService broadcastService, ISignatureService signatureService, ISmsAddressLookupService smsAddressLookupService, ISmsMessagingService smsMessagingService, IInstantMessagingService instantMessagingService, IRoomMetadataRepository roomRepository, IMeetingCacheService meetingCacheService, IExchangeRestChangeNotificationService exchangeRestChangeNotificationService)
        {
            _exchange = exchange;
            _graph = graph;
            _contextService = contextService;
            _dateTimeService = dateTimeService;
            _buildingRepository = buildingRepository;
            _floorRepository = floorRepository;
            _meetingRepository = meetingRepository;
            _broadcastService = broadcastService;
            _signatureService = signatureService;
            _smsAddressLookupService = smsAddressLookupService;
            _smsMessagingService = smsMessagingService;
            _instantMessagingService = instantMessagingService;
            _roomRepository = roomRepository;
            _meetingCacheService = meetingCacheService;
            _exchangeRestChangeNotificationService = exchangeRestChangeNotificationService;
        }

        public async Task<RoomInfo> GetStaticInfo(IRoom room)
        {
            var roomName = await _graph.GetUserDisplayName(room.RoomAddress);

            var canControl = CanControl(room);
            if (canControl )
            {
                // make sure we track rooms we're controlling
                _exchangeRestChangeNotificationService.TrackOrganization(room.OrganizationId);
            }

            var buildingTask = _buildingRepository.GetAsync(room.BuildingId);
            var floorTask = _floorRepository.GetAsync(room.FloorId);
            var building = (await buildingTask) ?? new BuildingEntity();
            var floor = (await floorTask) ?? new FloorEntity();

            return BuildRoomInfo(roomName, canControl, (RoomMetadataEntity)room, building, floor);
        }

        private bool CanControl(IRoom room)
        {
            return _contextService.CurrentDevice?.ControlledRoomIds?.Contains(room.Id) ?? false;
        }

        private RoomInfo BuildRoomInfo(string roomName, bool canControl, RoomMetadataEntity roomMetadata, BuildingEntity building, FloorEntity floor)
        {
            return new RoomInfo()
            {
                Id = roomMetadata.Id,
                CurrentTime = _dateTimeService.Now,
                DisplayName = (roomName ?? "").Replace("(Meeting Room ", "("),
                CanControl = canControl,
                Size = roomMetadata.Size,
                BuildingId = roomMetadata.BuildingId,
                BuildingName = building.Name,
                FloorId = roomMetadata.FloorId,
                Floor = floor.Number,
                FloorName = floor.Name,
                DistanceFromFloorOrigin = roomMetadata.DistanceFromFloorOrigin ?? new Point(),
                Equipment = roomMetadata.Equipment,
                HasControllableDoor = !string.IsNullOrEmpty(roomMetadata.GdoDeviceId),
                BeaconUid = roomMetadata.BeaconUid,
            };
        }

        private Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(IRoom room)
        {
            _exchangeRestChangeNotificationService.TrackOrganization(room.OrganizationId);
            var isTracked = true;

            return _meetingCacheService.GetUpcomingAppointmentsForRoom(room.RoomAddress, isTracked, () =>
            {
                return Task.Run(async () =>
                {
                    var apt = (await _exchange.GetCalendarEvents(room.RoomAddress, _dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2)))?.Value ?? new CalendarEntry[0];
                    if (_ignoreFree)
                    {
                        apt = apt.Where(i => i.ShowAs != ShowAs.Free).ToArray();
                    }

                    return (await BuildMeetings(room, apt)).AsEnumerable();
                });
            });
        }

        private async Task<Tuple<Meeting, CalendarEntry>> GetAppointmentForRoom(IRoom room, string uniqueId)
        {
            var apt = (await _exchange.GetCalendarEvent(room.RoomAddress, uniqueId))?.Value;
            if (null == apt)
            {
                return null;
            }

            return new Tuple<Meeting, CalendarEntry>((await BuildMeetings(room, new [] { apt })).Single(), apt);
        }

        private async Task<Meeting[]> BuildMeetings(IRoom room, CalendarEntry[] apt)
        {
            // short-circuit if we don't have any meetings
            if (!apt.Any())
            {
                return new Meeting[] {};
            }

            var meetings = (await _meetingRepository.GetMeetingInfoAsync(room.OrganizationId, apt.Select(i => i.Id).ToArray())).ToDictionary(i => i.UniqueId);
            return
                apt.Select(
                        i =>
                            BuildMeeting(i,
                                meetings.TryGetValue(i.Id) ?? new MeetingEntity() {Id = i.Id, OrganizationId = room.OrganizationId}))
                    .ToArray();
        }

        private Task<IEnumerable<Meeting>> GetSimpleUpcomingAppointmentsForRoom(IRoom room)
        {
            // TODO: why are there two of these....
            return GetUpcomingAppointmentsForRoom(room);
        }

        private Meeting BuildMeeting(CalendarEntry i, MeetingEntity meetingInfo)
        {
            return new Meeting()
            {
                UniqueId = i.Id,
                Subject = i.Sensitivity != Sensitivity.Normal ? i.Sensitivity.ToString() :
                    i.Subject != null && i.Subject.Trim() == i.Organizer?.EmailAddress?.Name.Trim() ? null : i.Subject,
                Start = i.Start.ToOffset().DateTime,
                End = i.End.ToOffset().DateTime,
                Organizer = i.Organizer?.EmailAddress?.Name,
                RequiredAttendees = i.Attendees?.Count(ii => ii.Type == "Required") ?? 0,
                OptionalAttendees = i.Attendees?.Count(ii => ii.Type == "Optional") ?? 0,
                ExternalAttendees = i.Attendees?.Count(IsExternalAttendee) ?? 0,
                IsStarted = meetingInfo.IsStarted,
                IsEndedEarly = meetingInfo.IsEndedEarly,
                IsCancelled = meetingInfo.IsCancelled,
                IsNotManaged = i.IsAllDay || Math.Abs(i.End.ToOffset().Subtract(i.Start.ToOffset()).TotalHours) >= 4, // all day events and events 4 hours or longer won't be auto-cancelled
            };
        }

        private bool IsExternalAttendee(Attendee attendee)
        {
            var address = attendee.EmailAddress?.Address?.ToLowerInvariant();
            return string.IsNullOrEmpty(address) || !_emailDomains.Any(emailDomain => address.EndsWith(emailDomain));
        }


        public async Task<RoomStatusInfo> GetStatus(IRoom room, bool isSimple = false)
        {
            var now = _dateTimeService.Now;
            var allMeetings = (isSimple ? await GetSimpleUpcomingAppointmentsForRoom(room) : await GetUpcomingAppointmentsForRoom(room))
                .OrderBy(i => i.Start).ToList();
            var upcomingMeetings = allMeetings.Where(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now);
            var meetings = upcomingMeetings
                    .Take(2)
                    .ToList();

            var prev = allMeetings.LastOrDefault(i => i.End < now);
            var current = meetings.FirstOrDefault();
            //_changeNotificationService.IsTrackedForChanges(room);
            var isTracked = true;

            var info = new RoomStatusInfo
            {
                IsTrackingChanges = isTracked,
                NearTermMeetings = allMeetings.ToArray(),
                PreviousMeeting = prev,
                CurrentMeeting = current,
                NextMeeting = meetings.Skip(1).FirstOrDefault(),
            };
            if (null == current)
            {
                info.Status = RoomStatus.Free;
            }
            else if (now < current.Start)
            {
                info.Status = current.IsStarted ? RoomStatus.Busy : RoomStatus.Free;
                info.NextChangeSeconds = current.Start.Subtract(now).TotalSeconds;
            }
            else
            {
                info.Status = current.IsStarted ? RoomStatus.Busy : RoomStatus.BusyNotConfirmed;
                info.NextChangeSeconds = current.End.Subtract(now).TotalSeconds;
            }

            if (info.Status == RoomStatus.Free)
            {
                info.RoomNextFreeInSeconds = 0;
            }
            else
            {
                var nextFree = now;
                foreach (var meeting in upcomingMeetings)
                {
                    if (nextFree < meeting.Start)
                    {
                        break;
                    }
                    nextFree = meeting.End;
                }
                info.RoomNextFreeInSeconds = nextFree.Subtract(now).TotalSeconds;
            }

            return info;
        }

        private async Task<Tuple<Meeting, CalendarEntry>> SecurityCheck(IRoom room, string uniqueId)
        {
            await SecurityCheck(room);
            var meeting = await GetAppointmentForRoom(room, uniqueId);
            if (null == meeting)
            {
                __log.InfoFormat("Unable to find meeting {0}", uniqueId);
                throw new Exception();
            }
            return meeting;
        }

        public async Task SecurityCheck(IRoom room)
        {
            var canControl = _contextService.CurrentDevice?.ControlledRoomIds?.Contains(room.Id) ?? false;
            if (!canControl)
            {
                __log.DebugFormat("Failing security check for {0}", room.Id);
                throw new UnauthorizedAccessException();
            }
        }

        public async Task StartMeeting(IRoom room, string uniqueId)
        {
            await SecurityCheck(room, uniqueId);
            __log.DebugFormat("Starting {0} for {1}", uniqueId, room.Id);
            _meetingRepository.StartMeeting(room.OrganizationId, uniqueId);
            await BroadcastUpdate(room);
        }

        public async Task<bool> StartMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(room, uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            __log.DebugFormat("Starting {0} for {1}", uniqueId, room.Id);
            _meetingRepository.StartMeeting(room.OrganizationId, uniqueId);
            await BroadcastUpdate(room);
            return true;
        }

        public async Task CancelMeeting(IRoom room, string uniqueId)
        {
            var meeting = await SecurityCheck(room, uniqueId);
            if (meeting.Item1.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            await _CancelMeeting(room, meeting.Item2);
        }

        public async Task<bool> CancelMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(room, uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            var meeting = await GetAppointmentForRoom(room, uniqueId);
            __log.DebugFormat("Abandoning {0} for {1}/{2}", uniqueId, room.RoomAddress, room.Id);
            await _CancelMeeting(room, meeting.Item2);
            return true;
        }

        private async Task _CancelMeeting(IRoom room, CalendarEntry meeting)
        {
            _meetingRepository.CancelMeeting(room.OrganizationId, meeting.Id);

            await _exchange.Truncate(room.RoomAddress, meeting, _dateTimeService.Now.TruncateToTheMinute());

            await SendEmail(room, meeting,
                string.Format("Your meeting '{0}' in {1} has been cancelled.", meeting.Subject, room.RoomAddress),
                "<p>If you want to keep the room, use the RoomNinja on the wall outside the room to start a new meeting ASAP.</p>");

            await BroadcastUpdate(room);
        }


        private async Task SendEmail(IRoom room, CalendarEntry meeting, string subject, string body)
        {
            var msg  = new Message()
            {
                From = new Recipient() { EmailAddress = new EmailAddress() { Address = room.RoomAddress } },
                ReplyTo = new []
                {
                    new Recipient() { EmailAddress = new EmailAddress() { Address = "noreply@" + room.RoomAddress.Split('@').Last() } },
                },
                Subject = subject,
                Body = new BodyContent()
                {
                    ContentType = "HTML",
                    Content = body,
                },
                ToRecipients = new Recipient[0],
            };

            msg.ToRecipients =
                new [] {  meeting.Organizer }.Concat(meeting.Attendees)
                .Where(i => !IsExternalAttendee(i))
                .Select(i => i?.EmailAddress)
                .Where(i => !string.IsNullOrEmpty(i?.Address))
                .Select(i => new Recipient { EmailAddress = i } )
                .ToArray();

            await _exchange.SendMessage(msg, room.RoomAddress);
        }

        public async Task WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl)
        {
            var meeting = await SecurityCheck(room, uniqueId);
            if (meeting.Item1.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            __log.DebugFormat("Warning {0} for {1}/{2}, which should start at {3}", uniqueId, room.RoomAddress, room.Id, meeting.Item2.Start);
            var startUrl = buildStartUrl(_signatureService.GetSignature(room, uniqueId));
            var cancelUrl = buildCancelUrl(_signatureService.GetSignature(room, uniqueId));
            await SendEmail(room, meeting.Item2, string.Format("WARNING: your meeting '{0}' in {1} is about to be cancelled.", meeting.Item2.Subject, meeting.Item2.Location?.DisplayName), "<p>Please start your meeting by using the RoomNinja on the wall outside the room or simply <a href='" + startUrl + "'>click here to START the meeting</a>.</p><p><a href='" + cancelUrl + "'>Click here to RELEASE the room</a> if you no longer need it so that others can use it.</p>");
        }

        public async Task AbandonMeeting(IRoom room, string uniqueId)
        {
            var meeting = await SecurityCheck(room, uniqueId);
            if (meeting.Item1.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            if (meeting.Item1.IsStarted)
            {
                throw new Exception("Cannot abandon a meeting that has started");
            }
            await _CancelMeeting(room, meeting.Item2);
        }

        public async Task EndMeeting(IRoom room, string uniqueId)
        {
            var meeting = await SecurityCheck(room, uniqueId);
            if (meeting.Item1.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.EndMeeting(room.OrganizationId, uniqueId);

            var now = _dateTimeService.Now.TruncateToTheMinute();
            await _exchange.Truncate(room.RoomAddress, meeting.Item2, now);

            await BroadcastUpdate(room);
        }

        public async Task StartNewMeeting(IRoom room, string title, DateTime endTime)
        {
            await SecurityCheck(room);
            var status = await GetStatus(room);
            if (status.Status != RoomStatus.Free)
            {
                throw new Exception("Room is not free");
            }


            var now = _dateTimeService.Now.TruncateToTheMinute();
            if (status.NextMeeting != null)
            {
                if (endTime > status.NextMeeting.Start)
                {
                    endTime = status.NextMeeting.Start;
                }
            }
            if (endTime.Subtract(now).TotalHours > 2)
            {
                throw new ApplicationException("Cannot create a meeting for more than 2 hours");
            }

            var item = await _exchange.CreateEvent(room.RoomAddress, new CalendarEntry
            {
                Start = new DateTimeReference() { DateTime = now.ToUniversalTime().ToString("o") },
                End = new DateTimeReference() { DateTime = endTime.ToUniversalTime().ToString("o") },
                Subject = title,
                Body = new BodyContent() {  Content = "Scheduled via conference room management system", ContentType = "Text" },
            });

            _meetingRepository.StartMeeting(room.OrganizationId, item.Id);
            await BroadcastUpdate(room);
        }

        public async Task MessageMeeting(IRoom room, string uniqueId)
        {
            var meeting = await SecurityCheck(room, uniqueId);
            if (meeting.Item1.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            var addresses = meeting.Item2.Attendees.Concat(new[] { meeting.Item2.Organizer })
                .Where(i => IsExternalAttendee(i) == false)
                .Select(i => i?.EmailAddress?.Address)
                .Where(i => !string.IsNullOrEmpty(i))
                .ToArray();

            var smsAddresses = _smsAddressLookupService.LookupAddresses(addresses);

            if (smsAddresses.Any())
            {
                _smsMessagingService.Send(smsAddresses, string.Format("Your meeting in {0} is over - please finish up ASAP - others are waiting outside.", meeting.Item2.Location?.DisplayName));
            }
            if (addresses.Any())
            {
                _instantMessagingService.SendMessage(addresses, string.Format("Meeting in {0} is over", meeting.Item2.Location?.DisplayName), string.Format("Your meeting in {0} is over - people for the next meeting are patiently waiting at the door. Please wrap up ASAP.", meeting.Item2.Location?.DisplayName), InstantMessagePriority.Urgent);
            }
        }

        public async Task<Dictionary<string, Tuple<RoomInfo, IRoom>>> GetInfoForRoomsInBuilding(string buildingId)
        {
            // repo data
            var building = _buildingRepository.Get(buildingId);
            var floors = _floorRepository.GetAllByBuilding(buildingId).ToDictionary(_ => _.Id);
            var roomMetadatas = _roomRepository.GetRoomInfosForBuilding(buildingId).ToDictionary(_ => _.RoomAddress);

            // get these started in parallel while we load data from the repositories
            var roomTasks = roomMetadatas.ToDictionary(i => i.Key, i => _graph.GetUserDisplayName(i.Key)).ToList();

            __log.DebugFormat("Started room load calls");

            // put it all together
            var results = new Dictionary<string, Tuple<RoomInfo, IRoom>>();
            foreach (var kvp in roomTasks)
            {
                var roomAddress = kvp.Key;
                var room = await kvp.Value;
                if (null == room)
                {
                    continue;
                }

                var roomMetadata = roomMetadatas.TryGetValue(roomAddress) ?? new RoomMetadataEntity();
                var canControl = CanControl(roomMetadata);
                var floor = floors.TryGetValue(roomMetadata.FloorId) ?? new FloorEntity();

                results.Add(roomAddress, new Tuple<RoomInfo, IRoom>(BuildRoomInfo(room, canControl, roomMetadata, building, floor), roomMetadata));
            }

            __log.DebugFormat("Room info build complete");

            return results;
        }

        private async Task BroadcastUpdate(IRoom room)
        {
            //_meetingCacheService.ClearUpcomingAppointmentsForRoom(room.RoomAddress);
            _broadcastService.BroadcastUpdate(_contextService.CurrentOrganization, room);
        }

    }
}
