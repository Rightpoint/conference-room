using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeConferenceRoomService : IConferenceRoomService
    {
        private readonly ExchangeService _exchangeService;
        private readonly IMeetingRepository _meetingRepository;
        private readonly ISecurityRepository _securityRepository;

        public ExchangeConferenceRoomService(ExchangeService exchangeService, IMeetingRepository meetingRepository, ISecurityRepository securityRepository)
        {
            _exchangeService = exchangeService;
            _meetingRepository = meetingRepository;
            _securityRepository = securityRepository;
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

        public IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(string roomAddress)
        {
            var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));
            var cal = CalendarFolder.Bind(_exchangeService, calId);
            var apt = cal.FindAppointments(new CalendarView(DateTime.Today, DateTime.Today.AddDays(2))).ToList();
            var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.UniqueId);
            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingInfo() { UniqueId = i.Id.UniqueId })).ToList();
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
