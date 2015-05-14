using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeConferenceRoomService : IConferenceRoomService
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ExchangeService _exchangeService;
        private readonly IMeetingRepository _meetingRepository;
        private readonly ISecurityRepository _securityRepository;
        private readonly IBroadcastService _broadcastService;

        public ExchangeConferenceRoomService(ExchangeService exchangeService, IMeetingRepository meetingRepository, ISecurityRepository securityRepository, IBroadcastService broadcastService)
        {
            _exchangeService = exchangeService;
            _meetingRepository = meetingRepository;
            _securityRepository = securityRepository;
            _broadcastService = broadcastService;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomList> GetRoomLists()
        {
            return _exchangeService.GetRoomLists().Select(i => new RoomList {Address = i.Address, Name = i.Name}).ToList();
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetRoomLists"/></param>
        /// <returns></returns>
        public IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress)
        {
            return _exchangeService.GetRooms(roomListAddress).Select(i => new Room {Address = i.Address, Name = i.Name}).ToList();
        }

        public object GetInfo(string roomAddress, string securityKey = null)
        {
            var roomInfo = _exchangeService.ResolveName(roomAddress).Single();
            return new
            {
                DisplayName = roomInfo.Mailbox.Name,
                SecurityStatus = _securityRepository.GetSecurityRights(roomAddress, securityKey),
            };
        }

        public IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(string roomAddress)
        {
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var apt = cal.FindAppointments(new CalendarView(DateTime.Today, DateTime.Today.AddDays(2))).ToList();
            var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.Id);
            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingInfo() { Id = i.Id.UniqueId })).ToList();
        }

        public RoomStatusInfo GetStatus(string roomAddress)
        {
            var now = DateTime.Now;
            var current = GetUpcomingAppointmentsForRoom(roomAddress)
                    .OrderBy(i => i.Start)
                    .FirstOrDefault(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now);

            if (null == current)
            {
                return new RoomStatusInfo
                {
                    Status = RoomStatus.Free,
                    NextChangeSeconds = 15 * 60,
                };
            }
            else if (now < current.Start)
            {
                return new RoomStatusInfo
                {
                    Status = RoomStatus.Free, 
                    NextChangeSeconds = Math.Min(15 * 60, current.Start.Subtract(now).TotalSeconds), 
                    Meeting = current
                };
            }
            else
            {
                return new RoomStatusInfo
                {
                    Status = current.IsStarted ? RoomStatus.Busy : RoomStatus.BusyNotConfirmed,
                    NextChangeSeconds = current.End.Subtract(now).TotalSeconds,
                    Meeting = current,
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
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var items = cal.FindItems(new SearchFilter.IsEqualTo(AppointmentSchema.Id, uniqueId), new ItemView(100)).Cast<Appointment>().ToList();
            items.ForEach(item =>
            {
                SendEmail(item, string.Format("WARNING: your meeting '{0}' is about to be cancelled.", item.Subject), "Use the conference room management device to start the meeting ASAP.");
            });
        }

        public void CancelMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId, securityKey);
            _meetingRepository.CancelMeeting(uniqueId);

            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var items = cal.FindItems(new SearchFilter.IsEqualTo(AppointmentSchema.Id, uniqueId), new ItemView(100)).Cast<Appointment>().ToList();
            items.ForEach(item =>
            {
                SendEmail(item, string.Format("Your meeting '{0}' has been cancelled.", item.Subject), "If you want to keep the room, use the conference room management device to start a new meeting ASAP.");
                item.Delete(DeleteMode.SoftDelete);
            });

            BroadcastUpdate(roomAddress);
        }

        public void EndMeeting(string roomAddress, string uniqueId, string securityKey)
        {
            SecurityCheck(roomAddress, uniqueId, securityKey);
            _meetingRepository.EndMeeting(uniqueId);

            var now = DateTime.Now.TruncateToTheMinute();
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var items = cal.FindItems(new SearchFilter.IsEqualTo(AppointmentSchema.Id, uniqueId), new ItemView(100)).Cast<Appointment>().ToList();
            items.ForEach(item =>
            {
                item.End = now;
                item.Update(ConflictResolutionMode.AlwaysOverwrite);
            });

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
            
            var now = DateTime.Now.TruncateToTheMinute();
            minutes = Math.Max(minutes, Math.Min(240, status.Meeting.ChainIfNotNull(m => (int?)m.Start.Subtract(now).TotalMinutes) ?? 240));

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
            return new Meeting { UniqueId = i.Id.UniqueId, Subject = i.Subject, Start = i.Start, End = i.End, Organizer = i.Organizer.Name, IsStarted = meetingInfo.IsStarted, IsEndedEarly = meetingInfo.IsEndedEarly, IsCancelled = meetingInfo.IsCancelled };
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
                var svc = new ExchangeService(ExchangeVersion.Exchange2010);
                svc.Credentials = new WebCredentials(username, password);
                svc.AutodiscoverUrl(username, url => new Uri(url).Scheme == "https");
                log.DebugFormat("Auto-discovery complete - found URL: {0}", svc.Url);
                return svc.Url.ToString();
            });

            return () =>
                new ExchangeService(ExchangeVersion.Exchange2010)
                {
                    Credentials = new WebCredentials(username, password),
                    Url = new Uri(svcUrl.Value),
                };

        }
    }
}
