using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.Exchange.WebServices.Data;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Models;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeEWS
{
    public class ExchangeConferenceRoomService : ISyncConferenceRoomService
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
        private readonly IFloorRepository _floorRepository;
        private readonly IContextService _contextService;
        private readonly IConferenceRoomDiscoveryService _exchangeConferenceRoomDiscoveryService;
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
            IFloorRepository floorRepository,
            IContextService contextService,
            IConferenceRoomDiscoveryService exchangeConferenceRoomDiscoveryService,
            ExchangeConferenceRoomServiceConfiguration config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
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
            _floorRepository = floorRepository;
            _contextService = contextService;
            _exchangeConferenceRoomDiscoveryService = exchangeConferenceRoomDiscoveryService;
            _ignoreFree = config.IgnoreFree;
            _useChangeNotification = config.UseChangeNotification;
            _impersonateForAllCalls = config.ImpersonateForAllCalls;
            _emailDomains = config.EmailDomains;
        }

        public RoomInfo GetStaticInfo(IRoom room)
        {
            var roomName = _exchangeConferenceRoomDiscoveryService.GetRoomName(room.RoomAddress).Result;
            if (null == roomName)
            {
                return null;
            }

            var canControl = CanControl(room);
            if (canControl && _useChangeNotification)
            {
                // make sure we track rooms we're controlling
                _changeNotificationService.TrackRoom(room, _exchangeServiceManager, _contextService.CurrentOrganization);
            }

            var building = _buildingRepository.Get(room.BuildingId) ?? new BuildingEntity();
            var floor = _floorRepository.Get(room.FloorId) ?? new FloorEntity();

            return BuildRoomInfo(roomName, canControl, (RoomMetadataEntity) room, building, floor);
        }

        public Dictionary<string, Tuple<RoomInfo, IRoom>> GetInfoForRoomsInBuilding(string buildingId)
        {
            // repo data
            var building = _buildingRepository.Get(buildingId);
            var floors = _floorRepository.GetAllByBuilding(buildingId).ToDictionary(_ => _.Id);
            var roomMetadatas = _roomRepository.GetRoomInfosForBuilding(buildingId).ToDictionary(_ => _.RoomAddress);

            // get these started in parallel while we load data from the repositories
            var roomTasks = roomMetadatas.ToDictionary(i => i.Key, i => _exchangeConferenceRoomDiscoveryService.GetRoomName(i.Key)).ToList();

            // put it all together
            var results = new Dictionary<string, Tuple<RoomInfo, IRoom>>();
            foreach (var kvp in roomTasks)
            {
                var roomAddress = kvp.Key;
                var room = kvp.Value.Result;
                if (null == room)
                {
                    continue;
                }

                var roomMetadata = roomMetadatas.TryGetValue(roomAddress) ?? new RoomMetadataEntity();
                var canControl = CanControl(roomMetadata);
                var floor = floors.TryGetValue(roomMetadata.FloorId) ?? new FloorEntity();

                results.Add(roomAddress, new Tuple<RoomInfo, IRoom>(BuildRoomInfo(room, canControl, roomMetadata, building, floor), roomMetadata));
            }

            return results;
        }

        public IEnumerable<Meeting> GetUpcomingAppointmentsForRoom(IRoom room)
        {
            var isTracked = _changeNotificationService.IsTrackedForChanges(room);
            return _meetingCacheService.GetUpcomingAppointmentsForRoom(room.RoomAddress, isTracked, () =>
            {
                return Task.Run(() =>
                {
                    try
                    {
                        return ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
                        {
                            var apt = svc.FindAppointments(WellKnownFolderName.Calendar, new CalendarView(_dateTimeService.Now.Date, _dateTimeService.Now.Date.AddDays(2)) { PropertySet = new PropertySet(AppointmentSchema.Id, AppointmentSchema.LegacyFreeBusyStatus)}).ToList();
                            __log.DebugFormat("Got {0} appointments for {1} via {2} with {3}", apt.Count, room.RoomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
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

                            var meetings = _meetingRepository.GetMeetingInfo(room.OrganizationId, apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.UniqueId);
                            return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingEntity() { Id = i.Id.UniqueId, OrganizationId = room.OrganizationId})).ToArray().AsEnumerable();
                        });
                    }
                    catch (ServiceResponseException ex)
                    {
                        CheckForAccessDenied(room, ex);
                        __log.DebugFormat("Unexpected error ({0}) getting appointments for {1}", ex.ErrorCode, room.RoomAddress);
                        throw;
                    }
                });
            }).Result;
        }

        public IEnumerable<Meeting> GetSimpleUpcomingAppointmentsForRoom(IRoom room)
        {
            var isTracked = _changeNotificationService.IsTrackedForChanges(room);
            var result = _meetingCacheService.TryGetUpcomingAppointmentsForRoom(room.RoomAddress, isTracked)?.Result;
            if (null == result)
            {
                try
                {
                    return ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
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
                        __log.DebugFormat("Got {0} appointments for {1} via {2} with {3}", apt.Count, room.RoomAddress, svc.GetHashCode(), svc.CookieContainer.GetCookieHeader(svc.Url));
                        if (_ignoreFree)
                        {
                            apt = apt.Where(i => i.LegacyFreeBusyStatus != LegacyFreeBusyStatus.Free).ToList();
                        }

                        // short-circuit if we don't have any meetings
                        if (!apt.Any())
                        {
                            return new Meeting[] { };
                        }
                        
                        var meetings = _meetingRepository.GetMeetingInfo(room.OrganizationId, apt.Select(i => i.Id.UniqueId).ToArray()).ToDictionary(i => i.Id);
                        return apt.Select(i => BuildMeeting(i, meetings.TryGetValue(i.Id.UniqueId) ?? new MeetingEntity() { Id = i.Id.UniqueId })).ToArray().AsEnumerable();
                    });
                }
                catch (ServiceResponseException ex)
                {
                    CheckForAccessDenied(room, ex);
                    __log.DebugFormat("Unexpected error ({0}) getting appointments for {1}", ex.ErrorCode, room.RoomAddress);
                    throw;
                }
            }
            return result;
        }
        private static void CheckForAccessDenied(IRoom room, ServiceResponseException ex)
        {
            if (ex.ErrorCode == ServiceError.ErrorFolderNotFound || ex.ErrorCode == ServiceError.ErrorNonExistentMailbox || ex.ErrorCode == ServiceError.ErrorAccessDenied)
            {
                __log.DebugFormat("Access denied ({0}) getting appointments for {1}/{2}", ex.ErrorCode, room.RoomAddress, room.Id);
                throw new AccessDeniedException("Folder/mailbox not found or access denied", ex);
            }
        }

        public RoomStatusInfo GetStatus(IRoom room, bool isSimple = false)
        {
            var now = _dateTimeService.Now;
            var allMeetings = (isSimple ? GetSimpleUpcomingAppointmentsForRoom(room) : GetUpcomingAppointmentsForRoom(room))
                .OrderBy(i => i.Start).ToList();
            var upcomingMeetings = allMeetings.Where(i => !i.IsCancelled && !i.IsEndedEarly && i.End > now);
            var meetings = upcomingMeetings
                    .Take(2)
                    .ToList();

            var prev = allMeetings.LastOrDefault(i => i.End < now);
            var current = meetings.FirstOrDefault();
            var isTracked = _changeNotificationService.IsTrackedForChanges(room);

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

        public void StartMeeting(IRoom room, string uniqueId)
        {
            SecurityCheck(room, uniqueId);
            __log.DebugFormat("Starting {0} for {1}", uniqueId, room.Id);
            _meetingRepository.StartMeeting(room.OrganizationId, uniqueId);
            BroadcastUpdate(room);
        }

        public bool StartMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(room, uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            __log.DebugFormat("Starting {0} for {1}", uniqueId, room.Id);
            _meetingRepository.StartMeeting(room.OrganizationId, uniqueId);
            BroadcastUpdate(room);
            return true;
        }

        public bool CancelMeetingFromClient(IRoom room, string uniqueId, string signature)
        {
            if (!_signatureService.VerifySignature(room, uniqueId, signature))
            {
                __log.ErrorFormat("Invalid signature: {0} for {1}", signature, uniqueId);
                return false;
            }
            __log.DebugFormat("Abandoning {0} for {1}/{2}", uniqueId, room.RoomAddress, room.Id);
            _CancelMeeting(room, uniqueId);
            return true;
        }

        public void WarnMeeting(IRoom room, string uniqueId, Func<string, string> buildStartUrl, Func<string, string> buildCancelUrl)
        {
            var meeting = SecurityCheck(room, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc => Appointment.Bind(svc, new ItemId(uniqueId)));
            __log.DebugFormat("Warning {0} for {1}/{2}, which should start at {3}", uniqueId, room.RoomAddress, room.Id, item.Start);
            var startUrl = buildStartUrl(_signatureService.GetSignature(room, uniqueId));
            var cancelUrl = buildCancelUrl(_signatureService.GetSignature(room, uniqueId));
            SendEmail(room, item, string.Format("WARNING: your meeting '{0}' in {1} is about to be cancelled.", item.Subject, item.Location), "<p>Please start your meeting by using the RoomNinja on the wall outside the room or simply <a href='" + startUrl + "'>click here to START the meeting</a>.</p><p><a href='" + cancelUrl + "'>Click here to RELEASE the room</a> if you no longer need it so that others can use it.</p>");
        }

        public void AbandonMeeting(IRoom room, string uniqueId)
        {
            var meeting = SecurityCheck(room, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            if (meeting.IsStarted)
            {
                throw new Exception("Cannot abandon a meeting that has started");
            }
            _CancelMeeting(room, uniqueId);
        }

        public void CancelMeeting(IRoom room, string uniqueId)
        {
            var meeting = SecurityCheck(room, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _CancelMeeting(room, uniqueId);
        }

        public void _CancelMeeting(IRoom room, string uniqueId)
        {
            _meetingRepository.CancelMeeting(room.OrganizationId, uniqueId);

            var item = ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
            {
                var appt = Appointment.Bind(svc, new ItemId(uniqueId));
                __log.DebugFormat("Cancelling {0} for {1}/{2}, which should start at {3}", uniqueId, room.RoomAddress, room.Id, appt.Start);
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

            SendEmail(room, item,
                string.Format("Your meeting '{0}' in {1} has been cancelled.", item.Subject, item.Location),
                "<p>If you want to keep the room, use the RoomNinja on the wall outside the room to start a new meeting ASAP.</p>");

            BroadcastUpdate(room);
        }

        public void EndMeeting(IRoom room, string uniqueId)
        {
            var meeting = SecurityCheck(room, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }
            _meetingRepository.EndMeeting(room.OrganizationId, uniqueId);

            var now = _dateTimeService.Now.TruncateToTheMinute();

            ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
            {
                var item = Appointment.Bind(svc, new ItemId(uniqueId));
                __log.DebugFormat("Ending {0} for {1}/{2}, which should start at {3}", uniqueId, room.RoomAddress, room.Id, item.Start);
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

            BroadcastUpdate(room);
        }

        public void MessageMeeting(IRoom room, string uniqueId)
        {
            var meeting = SecurityCheck(room, uniqueId);
            if (meeting.IsNotManaged)
            {
                throw new Exception("Cannot manage this meeting");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress,
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

        public void StartNewMeeting(IRoom room, string title, DateTime endTime)
        {
            SecurityCheck(room);
            var status = GetStatus(room);
            if (status.Status != RoomStatus.Free)
            {
                throw new Exception("Room is not free");
            }

            var item = ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
            {
                var calId = new FolderId(WellKnownFolderName.Calendar, new Mailbox(room.RoomAddress));

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
                __log.DebugFormat("Created {0} for {1}", appt.Id.UniqueId, room.RoomAddress);
                return appt;
            });

            _meetingRepository.StartMeeting(room.OrganizationId, item.Id.UniqueId);
            BroadcastUpdate(room);
        }

        private void BroadcastUpdate(IRoom room)
        {
            _meetingCacheService.ClearUpcomingAppointmentsForRoom(room.RoomAddress);
            _broadcastService.BroadcastUpdate(_contextService.CurrentOrganization, room);
        }

        private void SendEmail(IRoom room, Appointment item, string subject, string body)
        {
            ExchangeServiceExecuteWithImpersonationCheck(room.RoomAddress, svc =>
            {
                var msg = new EmailMessage(svc);
                msg.From = new EmailAddress(room.RoomAddress);
                msg.ReplyTo.Add("noreply@" + room.RoomAddress.Split('@').Last());
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

        private Meeting SecurityCheck(IRoom room, string uniqueId)
        {
            SecurityCheck(room);
            var meeting = GetUpcomingAppointmentsForRoom(room).FirstOrDefault(i => i.UniqueId == uniqueId);
            if (null == meeting)
            {
                __log.InfoFormat("Unable to find meeting {0}", uniqueId);
                throw new Exception();
            }
            return meeting;
        }

        public void SecurityCheck(IRoom room)
        {
            var canControl = _contextService.CurrentDevice?.ControlledRoomIds?.Contains(room.Id) ?? false;
            if (!canControl)
            {
                __log.DebugFormat("Failing security check for {0}", room.Id);
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

        private RoomInfo BuildRoomInfo(string roomName, bool canControl, RoomMetadataEntity roomMetadata, BuildingEntity building, FloorEntity floor)
        {
            return new RoomInfo()
            {
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

        private bool CanControl(IRoom room)
        {
            return _contextService.CurrentDevice?.ControlledRoomIds?.Contains(room.Id) ?? false;
        }
    }
}
