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

        public ExchangeRestConferenceRoomService(ExchangeRestWrapper exchange, GraphRestWrapper graph, IContextService contextService, IDateTimeService dateTimeService, IBuildingRepository buildingRepository, IFloorRepository floorRepository, IMeetingRepository meetingRepository, IBroadcastService broadcastService, ISignatureService signatureService)
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
        }

        public async Task<RoomInfo> GetStaticInfo(IRoom room)
        {
            var roomName = await _graph.GetUserDisplayName(room.RoomAddress);

            var canControl = CanControl(room);
            //if (canControl && _useChangeNotification)
            //{
            //    // make sure we track rooms we're controlling
            //    _changeNotificationService.TrackRoom(room, _exchangeServiceManager, _contextService.CurrentOrganization);
            //}

            var building = _buildingRepository.Get(room.BuildingId) ?? new BuildingEntity();
            var floor = _floorRepository.Get(room.FloorId) ?? new FloorEntity();

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


        private async Task<IEnumerable<Meeting>> GetUpcomingAppointmentsForRoom(IRoom room)
        {
            var apt = (await _exchange.GetCalendarEvents(room.RoomAddress, _dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2)))?.Value ?? new ExchangeRestWrapper.CalendarEntry[0];
            if (_ignoreFree)
            {
                apt = apt.Where(i => i.ShowAs != ExchangeRestWrapper.ShowAs.Free).ToArray();
            }

            // short-circuit if we don't have any meetings
            if (!apt.Any())
            {
                return new Meeting[] { };
            }

            var meetings = _meetingRepository.GetMeetingInfo(room.OrganizationId, apt.Select(i => i.Id).ToArray()).ToDictionary(i => i.UniqueId);
            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id) ?? new MeetingEntity() { Id = i.Id, OrganizationId = room.OrganizationId })).ToArray().AsEnumerable();
        }

        private Task<IEnumerable<Meeting>> GetSimpleUpcomingAppointmentsForRoom(IRoom room)
        {
            // TODO: why are there two of these....
            return GetUpcomingAppointmentsForRoom(room);
        }

        private Meeting BuildMeeting(ExchangeRestWrapper.CalendarEntry i, MeetingEntity meetingInfo)
        {
            return new Meeting()
            {
                UniqueId = i.Id,
                Subject = i.Sensitivity != ExchangeRestWrapper.Sensitivity.Normal ? i.Sensitivity.ToString() :
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

        private bool IsExternalAttendee(ExchangeRestWrapper.Attendee attendee)
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
            //var isTracked = _changeNotificationService.IsTrackedForChanges(room);
            var isTracked = false;

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

        private async Task<Meeting> SecurityCheck(IRoom room, string uniqueId)
        {
            await SecurityCheck(room);
            var meeting =(await GetUpcomingAppointmentsForRoom(room)).FirstOrDefault(i => i.UniqueId == uniqueId);
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
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            await _CancelMeeting(room, meeting);
        }

        public async Task<bool> CancelMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(room, uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            var meeting = (await GetUpcomingAppointmentsForRoom(room)).FirstOrDefault(i => i.UniqueId == uniqueId);
            __log.DebugFormat("Abandoning {0} for {1}/{2}", uniqueId, room.RoomAddress, room.Id);
            await _CancelMeeting(room, meeting);
            return true;
        }

        private async Task _CancelMeeting(IRoom room, Meeting meeting)
        {
            _meetingRepository.CancelMeeting(room.OrganizationId, meeting.UniqueId);

            await _exchange.Truncate(meeting.UniqueId);

            await SendEmail(room, meeting,
                string.Format("Your meeting '{0}' in {1} has been cancelled.", meeting.Subject, room.RoomAddress),
                "<p>If you want to keep the room, use the RoomNinja on the wall outside the room to start a new meeting ASAP.</p>");

            await BroadcastUpdate(room);
        }


        private async Task SendEmail(IRoom room, Meeting meeting, string subject, string body)
        {
            throw new NotImplementedException();
            //var msg = new EmailMessage(svc);
            //msg.From = new EmailAddress(room.RoomAddress);
            //msg.ReplyTo.Add("noreply@" + room.RoomAddress.Split('@').Last());
            //msg.Subject = subject;
            //msg.Body = body;
            //msg.Body.BodyType = BodyType.HTML;
            //__log.DebugFormat("Address: {0}, MailboxType: {1}", item.Organizer.Address, item.Organizer.MailboxType);
            //if (item.Organizer.MailboxType == MailboxType.Mailbox)
            //{
            //    msg.ToRecipients.Add(item.Organizer);
            //}
            //foreach (var x in item.RequiredAttendees.Concat(item.OptionalAttendees))
            //{
            //    __log.DebugFormat("Address: {0}, MailboxType: {1}, RoutingType: {2}", x.Address, x.MailboxType, x.RoutingType);
            //    if (x.RoutingType == "SMTP" && IsExternalAttendee(x) == false)
            //    {
            //        __log.DebugFormat("Also sending to {0} @ {1}", x.Name, x.Address);
            //        msg.CcRecipients.Add(x.Name, x.Address);
            //    }
            //}
            //msg.Send();
        }

        public Task WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl)
        {
            throw new NotImplementedException();
        }

        public Task AbandonMeeting(IRoom room, string uniqueId)
        {
            throw new NotImplementedException();
        }

        public Task EndMeeting(IRoom room, string uniqueId)
        {
            throw new NotImplementedException();
        }

        public Task StartNewMeeting(IRoom room, string title, DateTime endTime)
        {
            throw new NotImplementedException();
        }

        public Task MessageMeeting(IRoom room, string uniqueId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, Tuple<RoomInfo, IRoom>>> GetInfoForRoomsInBuilding(string buildingId)
        {
            throw new NotImplementedException();
        }

        private async Task BroadcastUpdate(IRoom room)
        {
            //_meetingCacheService.ClearUpcomingAppointmentsForRoom(room.RoomAddress);
            _broadcastService.BroadcastUpdate(_contextService.CurrentOrganization, room);
        }

    }
}
