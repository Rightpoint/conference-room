using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Rtc.Collaboration;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Models;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeConferenceRoomService : IConferenceRoomService
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IMeetingRepository _meetingRepository;
        private readonly IBroadcastService _broadcastService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IMeetingCacheService _meetingCacheService;
        private readonly IChangeNotificationService _changeNotificationService;
        private readonly IExchangeServiceManager _exchangeServiceManager;
        private readonly ISimpleTimedCache _simpleTimedCache;
        private readonly IInstantMessagingService _instantMessagingService;
        private readonly ISmsMessagingService _smsMessagingService;
        private readonly ISmsAddressLookupService _smsAddressLookupService;
        private readonly ISignatureService _signatureService;
        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IContextService _contextService;
        private readonly bool _ignoreFree;
        private readonly bool _useChangeNotification;
        private readonly bool _impersonateForAllCalls;
        private readonly string[] _emailDomains;

        public ExchangeConferenceRoomService(IMeetingRepository meetingRepository,
            IBroadcastService broadcastService,
            IDateTimeService dateTimeService,
            IMeetingCacheService meetingCacheService,
            IChangeNotificationService changeNotificationService,
            IExchangeServiceManager exchangeServiceManager,
            ISimpleTimedCache simpleTimedCache,
            IInstantMessagingService instantMessagingService,
            ISmsMessagingService smsMessagingService,
            ISmsAddressLookupService smsAddressLookupService,
            ISignatureService signatureService,
            IRoomMetadataRepository roomRepository,
            IBuildingRepository buildingRepository,
            IContextService contextService,
            ExchangeConferenceRoomServiceConfiguration config)
        {
            _meetingRepository = meetingRepository;
            _broadcastService = broadcastService;
            _dateTimeService = dateTimeService;
            _meetingCacheService = meetingCacheService;
            _changeNotificationService = changeNotificationService;
            _exchangeServiceManager = exchangeServiceManager;
            _simpleTimedCache = simpleTimedCache;
            _instantMessagingService = instantMessagingService;
            _smsMessagingService = smsMessagingService;
            _smsAddressLookupService = smsAddressLookupService;
            _signatureService = signatureService;
            _roomRepository = roomRepository;
            _buildingRepository = buildingRepository;
            _contextService = contextService;
            _ignoreFree = config.IgnoreFree;
            _useChangeNotification = config.UseChangeNotification;
            _impersonateForAllCalls = config.ImpersonateForAllCalls;
            _emailDomains = config.EmailDomains;
        }

        /// <summary>
        /// Get all room lists defined on the Exchange server.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RoomList> GetRoomLists()
        {
            return _simpleTimedCache.GetCachedValue("RoomLists", TimeSpan.FromHours(24),
                () => Task.FromResult(_exchangeServiceManager.Execute(string.Empty, svc => svc.GetRoomLists()
                    .Select(i => new RoomList {Address = i.Address, Name = i.Name})
                    .ToArray()))).Result;
        }

        /// <summary>
        /// Gets all the rooms in the specified room list.
        /// </summary>
        /// <param name="roomListAddress">The room list address returned from <see cref="GetRoomLists"/></param>
        /// <returns></returns>
        public IEnumerable<Room> GetRoomsFromRoomList(string roomListAddress)
        {
            return _simpleTimedCache.GetCachedValue("Rooms_" + roomListAddress,
                TimeSpan.FromHours(24),
                () => Task.FromResult(_exchangeServiceManager.Execute(string.Empty, svc => svc.GetRooms(roomListAddress)
                    .Select(i => new Room {Address = i.Address, Name = i.Name})
                    .ToArray()))).Result;
        }

        public RoomInfo GetInfo(string roomAddress = null)
        {
            var room = _simpleTimedCache.GetCachedValue("RoomInfo_" + roomAddress,
                TimeSpan.FromHours(24),
                () => Task.FromResult(ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc => svc.ResolveName(roomAddress).SingleOrDefault()))).Result;

            if (null == room)
            {
                return null;
            }

            var canControl = _contextService.CurrentDevice?.ControlledRoomAddresses?.Contains(roomAddress) ?? false;
            if (canControl && _useChangeNotification)
            {
                // make sure we track rooms we're controlling
                _changeNotificationService.TrackRoom(roomAddress, _exchangeServiceManager);
            }

            var roomMetadata = _roomRepository.GetRoomInfo(roomAddress, _contextService.CurrentOrganization?.Id) ?? new RoomMetadataEntity();
            var building = _buildingRepository.Get(roomMetadata.BuildingId) ?? new BuildingEntity();
            var floor = building.Floors?.SingleOrDefault(_ => _.Floor == roomMetadata.Floor) ?? new FloorEntity();

            return new RoomInfo()
            {
                CurrentTime = _dateTimeService.Now,
                DisplayName = (room.Mailbox.Name ?? "").Replace("(Meeting Room ", "("),
                CanControl = canControl,
                Size = roomMetadata.Size,
                BuildingId = roomMetadata.BuildingId,
                BuildingName = building.Name,
                Floor = roomMetadata.Floor,
                FloorName = floor.Name,
                DistanceFromFloorOrigin = roomMetadata.DistanceFromFloorOrigin ?? new Point(),
                Equipment = roomMetadata.Equipment,
                HasControllableDoor = !string.IsNullOrEmpty(roomMetadata.GdoDeviceId),
                BeaconUid = roomMetadata.BeaconUid,
            };
        }

        public IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(string roomAddress)
        {
            var isTracked = _changeNotificationService.IsTrackedForChanges(roomAddress);
            return _meetingCacheService.GetUpcomingAppointmentsForRoom(roomAddress, isTracked, () =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        return ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
                        {
                            var apt = svc.FindAppointments(WellKnownFolderName.Calendar, new CalendarView(_dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2)) { PropertySet = new PropertySet(AppointmentSchema.Id, AppointmentSchema.LegacyFreeBusyStatus)}).ToList();
                            __log.DebugFormat("Got {0} appointments for {1} via {2} with {3}", apt.Count, roomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
                            if (_ignoreFree)
                            {
                                apt = apt.Where(i => i.LegacyFreeBusyStatus != LegacyFreeBusyStatus.Free).ToList();
                            }

                            // short-circuit if we don't have any meetings
                            if (!apt.Any())
                            {
                                return new Meeting[] {};
                            }

                            // now that we have the items, load the data (can't load attendees in the FindAppointments call...)
                            svc.LoadPropertiesForItems(apt, new PropertySet(
                                AppointmentSchema.Id,
                                AppointmentSchema.Subject,
                                AppointmentSchema.Sensitivity,
                                AppointmentSchema.Organizer,
                                AppointmentSchema.Start,
                                AppointmentSchema.End,
                                AppointmentSchema.IsAllDayEvent,
                                AppointmentSchema.RequiredAttendees, 
                                AppointmentSchema.OptionalAttendees));

                            var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.Id);
                            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingEntity() { Id = i.Id.UniqueId })).ToArray().AsEnumerable();
                        });
                    }
                    catch (ServiceResponseException ex)
                    {
                        CheckForAccessDenied(roomAddress, ex);
                        __log.DebugFormat("Unexpected error ({0}) getting appointments for {1}", ex.ErrorCode, roomAddress);
                        throw;
                    }
                });
            }).Result;
        }

        public IEnumerable<Meeting> GetSimpleUpcomingAppointmentsForRoom(string roomAddress)
        {
            var isTracked = _changeNotificationService.IsTrackedForChanges(roomAddress);
            var result = _meetingCacheService.TryGetUpcomingAppointmentsForRoom(roomAddress, isTracked)?.Result;
            if (null == result)
            {
                try
                {
                    return ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
                    {
                        var apt = svc.FindAppointments(WellKnownFolderName.Calendar, new CalendarView(_dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2))
                        {
                            // load enough properties that we don't have to make a second trip to the server
                            PropertySet = new PropertySet(
                                AppointmentSchema.Id,
                                AppointmentSchema.LegacyFreeBusyStatus,
                                AppointmentSchema.Start,
                                AppointmentSchema.End,
                                AppointmentSchema.IsAllDayEvent
                            )
                        }).ToList();
                        __log.DebugFormat("Got {0} appointments for {1} via {2} with {3}", apt.Count, roomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
                        if (_ignoreFree)
                        {
                            apt = apt.Where(i => i.LegacyFreeBusyStatus != LegacyFreeBusyStatus.Free).ToList();
                        }

                        // short-circuit if we don't have any meetings
                        if (!apt.Any())
                        {
                            return new Meeting[] { };
                        }
                        
                        var meetings = _meetingRepository.GetMeetingInfo(apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.Id);
                        return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingEntity() { Id = i.Id.UniqueId })).ToArray().AsEnumerable();
                    });
                }
                catch (ServiceResponseException ex)
                {
                    CheckForAccessDenied(roomAddress, ex);
                    __log.DebugFormat("Unexpected error ({0}) getting appointments for {1}", ex.ErrorCode, roomAddress);
                    throw;
                }
            }
            return result;
        }
        private static void CheckForAccessDenied(string roomAddress, ServiceResponseException ex)
        {
            if (ex.ErrorCode == ServiceError.ErrorFolderNotFound || ex.ErrorCode == ServiceError.ErrorNonExistentMailbox || ex.ErrorCode == ServiceError.ErrorAccessDenied)
            {
                __log.DebugFormat("Access denied ({0}) getting appointments for {1}", ex.ErrorCode, roomAddress);
                throw new AccessDeniedException("Folder/mailbox not found or access denied", ex);
            }
        }

        public RoomStatusInfo GetStatus(string roomAddress, bool isSimple = false)
        {
            var now = _dateTimeService.Now;
            var allMeetings = (isSimple ? GetSimpleUpcomingAppointmentsForRoom(roomAddress) : GetUpcomingAppointmentsForRoom(roomAddress))
                .OrderBy(i => i.Start).ToList();
            var upcomingMeetings = allMeetings.Where(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now);
            var meetings = upcomingMeetings
                    .Take(2)
                    .ToList();

            var prev = allMeetings.LastOrDefault(i => i.End < now);
            var current = meetings.FirstOrDefault();
            var isTracked = _changeNotificationService.IsTrackedForChanges(roomAddress);

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

        public void StartMeeting(string roomAddress, string uniqueId)
        {
            SecurityCheck(roomAddress, uniqueId);
            __log.DebugFormat("Starting {0} for {1}", uniqueId, roomAddress);
            _meetingRepository.StartMeeting(uniqueId);
            BroadcastUpdate(roomAddress);
        }

        public bool StartMeetingFromClient(string roomAddress, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            __log.DebugFormat("Starting {0} for {1}", uniqueId, roomAddress);
            _meetingRepository.StartMeeting(uniqueId);
            BroadcastUpdate(roomAddress);
            return true;
        }

        public bool CancelMeetingFromClient(string roomAddress, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            __log.DebugFormat("Abandoning {0} for {1}", uniqueId, roomAddress);
            CancelMeeting(roomAddress, uniqueId);
            return true;
        }

        public void WarnMeeting(string roomAddress, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc => Appointment.Bind(svc, new ItemId(uniqueId)));
            __log.DebugFormat("Warning {0} for {1}, which should start at {2}", uniqueId, roomAddress, item.Start);
            var startUrl = buildStartUrl(_signatureService.GetSignature(uniqueId));
            var cancelUrl = buildCancelUrl(_signatureService.GetSignature(uniqueId));
            SendEmail(roomAddress, item, string.Format("WARNING: your meeting '{0}' in {1} is about to be cancelled.", item.Subject, item.Location), "<p>Please start your meeting by using the RoomNinja on the wall outside the room or simply <a href='" + startUrl + "'>click here to START the meeting</a>.</p><p><a href='" + cancelUrl + "'>Click here to RELEASE the room</a> if you no longer need it so that others can use it.</p>");
        }

        public void CancelMeeting(string roomAddress, string uniqueId)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.CancelMeeting(uniqueId);

            var item = ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
            {
                var appt = Appointment.Bind(svc, new ItemId(uniqueId));
                __log.DebugFormat("Cancelling {0} for {1}, which should start at {2}", uniqueId, roomAddress, appt.Start);
                var now = _dateTimeService.Now.TruncateToTheMinute();
                if (now >= appt.Start)
                {
                    appt.End = now;
                }
                else
                {
                    appt.End = appt.Start;
                }
                appt.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToNone);
                return appt;
            });

            SendEmail(roomAddress, item,
                string.Format("Your meeting '{0}' in {1} has been cancelled.", item.Subject, item.Location),
                "<p>If you want to keep the room, use the RoomNinja on the wall outside the room to start a new meeting ASAP.</p>");

            BroadcastUpdate(roomAddress);
        }

        public void EndMeeting(string roomAddress, string uniqueId)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.EndMeeting(uniqueId);

            var now = _dateTimeService.Now.TruncateToTheMinute();

            ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
            {
                var item = Appointment.Bind(svc, new ItemId(uniqueId));
                __log.DebugFormat("Ending {0} for {1}, which should start at {2}", uniqueId, roomAddress, item.Start);
                if (now >= item.Start)
                {
                    item.End = now;
                }
                else
                {
                    item.End = item.Start;
                }
                item.Update(ConflictResolutionMode.AlwaysOverwrite, SendInvitationsOrCancellationsMode.SendToNone);
            });

            BroadcastUpdate(roomAddress);
        }

        public void MessageMeeting(string roomAddress, string uniqueId)
        {
            var meeting = SecurityCheck(roomAddress, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(roomAddress,
                svc =>
                    Appointment.Bind(svc, new ItemId(uniqueId),
                        new PropertySet(AppointmentSchema.RequiredAttendees,
                            AppointmentSchema.OptionalAttendees,
                            AppointmentSchema.Location)));
            var addresses = item.RequiredAttendees.Concat(item.OptionalAttendees)
                .Where(i => i.Address != null &&
                            IsExternalAttendee(i) == false)
                .Select(i => i.Address)
                .ToArray();

            var smsAddresses = _smsAddressLookupService.LookupAddresses(addresses);

            if (smsAddresses.Any())
            {
                _smsMessagingService.Send(smsAddresses, string.Format("Your meeting in {0} is over - please finish up ASAP - others are waiting outside.", item.Location));
            }
            if (addresses.Any())
            {
                _instantMessagingService.SendMessage(addresses, string.Format("Meeting in {0} is over", item.Location), string.Format("Your meeting in {0} is over - people for the next meeting are patiently waiting at the door. Please wrap up ASAP.", item.Location), InstantMessagePriority.Urgent);
            }
        }

        public void StartNewMeeting(string roomAddress, string title, DateTime endTime)
        {
            SecurityCheck(roomAddress);
            var status = GetStatus(roomAddress);
            if (status.Status != RoomStatus.Free)
            {
                throw new Exception("Room is not free");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
            {
                var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(roomAddress));

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

                var appt = new Appointment(svc);
                appt.Start = now;
                appt.End = endTime;
                appt.Subject = title;
                appt.Body = "Scheduled via conference room management system";
                appt.Save(calId, SendInvitationsMode.SendToNone);
                __log.DebugFormat("Created {0} for {1}", appt.Id.UniqueId, roomAddress);
                return appt;
            });

            _meetingRepository.StartMeeting(item.Id.UniqueId);
            BroadcastUpdate(roomAddress);
        }

        private void BroadcastUpdate(string roomAddress)
        {
            _meetingCacheService.ClearUpcomingAppointmentsForRoom(roomAddress);
            _broadcastService.BroadcastUpdate(roomAddress);
        }

        private void SendEmail(string roomAddress, Appointment item, string subject, string body)
        {
            ExchangeServiceExecuteWithImpersonationCheck(roomAddress, svc =>
            {
                var msg = new EmailMessage(svc);
                msg.From = new EmailAddress(roomAddress);
                msg.ReplyTo.Add("noreply@" + roomAddress.Split('@').Last());
                msg.Subject = subject;
                msg.Body = body;
                msg.Body.BodyType = BodyType.HTML;
                __log.DebugFormat("Address: {0}, MailboxType: {1}", item.Organizer.Address, item.Organizer.MailboxType);
                if (item.Organizer.MailboxType == MailboxType.Mailbox)
                {
                    msg.ToRecipients.Add(item.Organizer);
                }
                foreach (var x in item.RequiredAttendees.Concat(item.OptionalAttendees))
                {
                    __log.DebugFormat("Address: {0}, MailboxType: {1}, RoutingType: {2}", x.Address, x.MailboxType, x.RoutingType);
                    if (x.RoutingType == "SMTP" && IsExternalAttendee(x) == false)
                    {
                        __log.DebugFormat("Also sending to {0} @ {1}", x.Name, x.Address);
                        msg.CcRecipients.Add(x.Name, x.Address);
                    }
                }
                msg.Send();
            });
        }

        private Meeting SecurityCheck(string roomAddress, string uniqueId)
        {
            SecurityCheck(roomAddress);
            var meeting = GetUpcomingAppointmentsForRoom(roomAddress).FirstOrDefault(i => i.UniqueId == uniqueId);
            if (null == meeting)
            {
                throw new Exception();
            }
            return meeting;
        }

        public void SecurityCheck(string roomAddress)
        {
            var canControl = _contextService.CurrentDevice?.ControlledRoomAddresses?.Contains(roomAddress) ?? false;
            if (!canControl)
            {
                throw new UnauthorizedAccessException();
            }
        }

        private bool IsExternalAttendee(Attendee attendee)
        {
            if (attendee.Address == null)
            {
                return true;
            }
            else
            {
                foreach (var emailDomain in _emailDomains)
                {
                    if (attendee.Address.ToLowerInvariant().EndsWith(emailDomain))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private Meeting BuildMeeting(Appointment i, MeetingEntity meetingInfo)
        {
            var fields = i.GetLoadedPropertyDefinitions().OfType<PropertyDefinition>().ToLookup(x => x.Name);

            var externalAttendees = fields.Contains("RequiredAttendees") && fields.Contains("OptionalAttendees") ?
                i.RequiredAttendees.Concat(i.OptionalAttendees).Count(IsExternalAttendee) : 0;

            return new Meeting()
            {
                UniqueId = i.Id.UniqueId,
                Subject = fields.Contains("Sensitivity") && fields.Contains("Subject") ? (
                    i.Sensitivity != Sensitivity.Normal ? i.Sensitivity.ToString() :
                    i.Subject != null && i.Subject.Trim() == i.Organizer.Name.Trim() ? null : i.Subject
                    ) : null,
                Start = i.Start,
                End = i.End,
                Organizer = fields.Contains("Organizer") ? i.Organizer.Name : string.Empty,
                RequiredAttendees = fields.Contains("RequiredAttendees") ? i.RequiredAttendees.Count : 0,
                OptionalAttendees = fields.Contains("OptionalAttendees") ? i.OptionalAttendees.Count : 0,
                ExternalAttendees = externalAttendees,
                IsStarted = meetingInfo.IsStarted,
                IsEndedEarly = meetingInfo.IsEndedEarly,
                IsCancelled = meetingInfo.IsCancelled,
                IsNotManaged = i.IsAllDayEvent || Math.Abs(i.End.Subtract(i.Start).TotalHours) > 6, // all day events and events longer than 6 hours won't be auto-cancelled
            };
        }

        private TResult ExchangeServiceExecuteWithImpersonationCheck<TResult>(string roomAddress, Func<ExchangeService, TResult> action)
        {
            var targetUser = _impersonateForAllCalls
                ? roomAddress
                : string.Empty;
            return _exchangeServiceManager.Execute(targetUser, action);
        }

        private void ExchangeServiceExecuteWithImpersonationCheck(string roomAddress, Action<ExchangeService> action)
        {
            var targetUser = _impersonateForAllCalls
                ? roomAddress
                : string.Empty;
            _exchangeServiceManager.Execute(targetUser, action);
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
                __log.DebugFormat("serviceUrl wasn't configured in appSettings, running auto-discovery");
                var svc = new ExchangeService(ExchangeVersion.Exchange2010_SP1);
                svc.Credentials = new WebCredentials(username, password);
                svc.PreAuthenticate = true;
                svc.AutodiscoverUrl(username, url => new Uri(url).Scheme == "https");
                __log.DebugFormat("Auto-discovery complete - found URL: {0}", svc.Url);
                return svc.Url.ToString();
            });

            return () =>
                new ExchangeService(ExchangeVersion.Exchange2010_SP1)
                {
                    Credentials = new WebCredentials(username, password),
                    Url = new Uri(svcUrl.Value),
                    PreAuthenticate = true,
                };

        }
    }
}
