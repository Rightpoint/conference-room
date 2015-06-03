using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeConferenceRoomService : IConferenceRoomService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ExchangeService _exchangeService;
        private readonly IMeetingRepository _meetingRepository;
        private readonly ISecurityRepository _securityRepository;
        private readonly IBroadcastService _broadcastService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMeetingCacheService _meetingCacheService;

        public ExchangeConferenceRoomService(ExchangeService exchangeService, IMeetingRepository meetingRepository, ISecurityRepository securityRepository, IBroadcastService broadcastService, IDateTimeService dateTimeService, IMeetingCacheService meetingCacheService)
        {
            _exchangeService = exchangeService;
            _meetingRepository = meetingRepository;
            _securityRepository = securityRepository;
            _broadcastService = broadcastService;
            _dateTimeService = dateTimeService;
            _meetingCacheService = meetingCacheService;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomList> GetRoomLists()
        {
            return _exchangeService.GetRoomLists().Select(i => new RoomList { Address = i.Address, Name = i.Name }).ToList();
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetRoomLists"/></param>
        /// <returns></returns>
        public IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress)
        {
            return _exchangeService.GetRooms(roomListAddress).Select(i => new Room { Address = i.Address, Name = i.Name }).ToList();
        }

        public object GetInfo(string roomAddress, string securityKey = null)
        {
            var roomInfo = _exchangeService.ResolveName(roomAddress).Single();
            return new
            {
                CurrentTime = _dateTimeService.Now,
                DisplayName = roomInfo.Mailbox.Name,
                SecurityStatus = _securityRepository.GetSecurityRights(roomAddress, securityKey),
            };
        }

        public void RequestAccess(string roomAddress, string securityKey, string clientInfo)
        {
            _securityRepository.RequestAccess(roomAddress, securityKey, clientInfo);
        }

        public IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(string roomAddress)
        {
            return _meetingCacheService.GetUpcomingAppointmentsForRoom(roomAddress, () =>
            {
                return Task.Run(() =>
                {
                    var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
                    var cal = CalendarFolder.Bind(_exchangeService, calId);
                    var apt = cal.FindAppointments(new CalendarView(_dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2))).ToList();
                    var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.Id);
                    return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingInfo() { Id = i.Id.UniqueId })).ToArray().AsEnumerable();
                });
            }).Result;
        }

        public RoomStatusInfo GetStatus(string roomAddress)
        {
            var now = _dateTimeService.Now;
            var allMeetings = GetUpcomingAppointmentsForRoom(roomAddress)
                .OrderBy(i => i.Start).ToList();
            var meetings = allMeetings
                    .Where(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now)
                    .Take(2)
                    .ToList();
            var current = meetings.FirstOrDefault();

            if (null == current)
            {
                return new RoomStatusInfo
                {
                    Status = RoomStatus.Free,
                    NearTermMeetings = allMeetings.ToArray(),
                };
            }
            else if (now < current.Start)
            {
                return new RoomStatusInfo
                {
                    Status = current.IsStarted ? RoomStatus.Busy : RoomStatus.Free,
                    NextChangeSeconds = current.Start.Subtract(now).TotalSeconds,
                    CurrentMeeting = current,
                    NextMeeting = meetings.Skip(1).FirstOrDefault(),
                    NearTermMeetings = allMeetings.ToArray(),
                };
            }
            else
            {
                return new RoomStatusInfo
                {
                    Status = current.IsStarted ? RoomStatus.Busy : RoomStatus.BusyNotConfirmed,
                    NextChangeSeconds = current.End.Subtract(now).TotalSeconds,
                    CurrentMeeting = current,
                    NextMeeting = meetings.Skip(1).FirstOrDefault(),
                    NearTermMeetings = allMeetings.ToArray(),
                };
            }
        }

        public void StartMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            SecurityCheck(roomAddress, uniqueId, securityKey);
            _meetingRepository.StartMeeting(uniqueId);
            BroadcastUpdate(roomAddress);
        }

        public void WarnMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId, securityKey);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var item = Appointment.Bind(_exchangeService, new ItemId(uniqueId));
            SendEmail(item, string.Format("WARNING: your meeting '{0}' in {1} is about to be cancelled.", item.Subject, item.Location), "Use the conference room management device to start the meeting ASAP.");
        }

        public void CancelMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId, securityKey);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.CancelMeeting(uniqueId);

            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var item = Appointment.Bind(_exchangeService, new ItemId(uniqueId));
            SendEmail(item, string.Format("Your meeting '{0}' in {1} has been cancelled.", item.Subject, item.Location), "If you want to keep the room, use the conference room management device to start a new meeting ASAP.");
            item.Delete(DeleteMode.SoftDelete);

            BroadcastUpdate(roomAddress);
        }

        public void EndMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId, securityKey);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.EndMeeting(uniqueId);

            var now = _dateTimeService.Now.TruncateToTheMinute();
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var item = Appointment.Bind(_exchangeService, new ItemId(uniqueId));
            if (now >= item.Start)
            {
                item.End = now;
            }
            else
            {
                item.End = item.Start;
            }
            item.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToNone);

            BroadcastUpdate(roomAddress);
        }

        public void StartNewMeeting(string roomAddress, string securityKey, string title, int minutes)
        {
            if (_securityRepository.GetSecurityRights(roomAddress, securityKey) != SecurityStatus.Granted)
            {
                throw new UnauthorizedAccessException();
            }
            var status = GetStatus(roomAddress);
            if (status.Status != RoomStatus.Free)
            {
                throw new Exception("Room is not free");
            }
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));

            var now = _dateTimeService.Now.TruncateToTheMinute();
            minutes = Math.Max(0, Math.Min(minutes, Math.Min(120, status.NextMeeting.ChainIfNotNull(m => (int?)m.Start.Subtract(now).TotalMinutes) ?? 120)));

            var appt = new Appointment(_exchangeService);
            appt.Start = now;
            appt.End = now.AddMinutes(minutes);
            appt.Subject = title;
            appt.Body = "Scheduled via conference room management system";
            appt.Save(calId, SendInvitationsMode.SendToNone);

            _meetingRepository.StartMeeting(appt.Id.UniqueId);
            BroadcastUpdate(roomAddress);
        }

        private void BroadcastUpdate(string roomAddress)
        {
            _meetingCacheService.ClearUpcomingAppointmentsForRoom(roomAddress);
            _broadcastService.BroadcastUpdate(roomAddress);
        }

        private void SendEmail(Appointment item, string subject, string body)
        {
            var msg = new EmailMessage(_exchangeService);
            msg.Subject = subject;
            msg.Body = body;
            log.DebugFormat("Address: {0}, MailboxType: {1}", item.Organizer.Address, item.Organizer.MailboxType);
            if (item.Organizer.MailboxType == MailboxType.Mailbox)
            {
                msg.ToRecipients.Add(item.Organizer);
            }
            foreach (var x in item.RequiredAttendees.Concat(item.OptionalAttendees))
            {
                log.DebugFormat("Address: {0}, MailboxType: {1}", x.Address, x.MailboxType);
                msg.CcRecipients.Add(x);
            }
            msg.Send();
        }

        private Meeting SecurityCheck(string roomAddress, string uniqueId, string securityKey)
        {
            if (_securityRepository.GetSecurityRights(roomAddress, securityKey) != SecurityStatus.Granted)
            {
                throw new UnauthorizedAccessException();
            }
            var meeting = GetUpcomingAppointmentsForRoom(roomAddress).FirstOrDefault(i => i.UniqueId == uniqueId);
            if (null == meeting)
            {
                throw new Exception();
            }
            return meeting;
        }

        private static Meeting BuildMeeting(Appointment i, MeetingInfo meetingInfo)
        {
            return new Meeting
            {
                UniqueId = i.Id.UniqueId,
                Subject = i.Subject,
                Start = i.Start,
                End = i.End,
                Organizer = i.Organizer.Name,
                IsStarted = meetingInfo.IsStarted,
                IsEndedEarly = meetingInfo.IsEndedEarly,
                IsCancelled = meetingInfo.IsCancelled,
                IsNotManaged = i.IsAllDayEvent,
            };
        }

        public static Func<ExchangeService> GetExchangeServiceBuilder(string username, string password, string serviceUrl)
        {
            // if we don't get a service URL in our configuration, run auto-discovery the first time we need it
            var svcUrl = new Lazy<string>(() =>
            {
                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    return serviceUrl;
                }
                var log = log4net.LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
                log.DebugFormat("serviceUrl wasn't configured in appSettings, running auto-discovery");
                var svc = new ExchangeService(ExchangeVersion.Exchange2010_SP1);
                svc.Credentials = new WebCredentials(username, password);
                svc.AutodiscoverUrl(username, url => new Uri(url).Scheme == "https");
                log.DebugFormat("Auto-discovery complete - found URL: {0}", svc.Url);
                return svc.Url.ToString();
            });

            return () =>
                new ExchangeService(ExchangeVersion.Exchange2010_SP1)
                {
                    Credentials = new WebCredentials(username, password),
                    Url = new Uri(svcUrl.Value),
                };

        }
    }
}
