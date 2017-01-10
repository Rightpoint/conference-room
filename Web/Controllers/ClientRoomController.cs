using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using log4net;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
{
    /// <summary>
    /// Operations dealing with a room
    /// </summary>
    [RoutePrefix("api/client-room")]
    public class ClientRoomController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IRoomMetadataRepository _roomRepository;
        private readonly IIOCContainer _container;

        public ClientRoomController(IRoomMetadataRepository roomRepository, IIOCContainer container)
            : base(__log)
        {
            _roomRepository = roomRepository;
            _container = container;
        }

        /// <summary>
        /// Marks a meeting as started
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="signature">The signature of the uniqueId - indicating it's allowed to do this</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/startFromClient", Name = "StartFromClient")]
        public string GetStartMeeting(string roomId, string uniqueId, string signature)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            using (var child = _container.CreateChildContainer())
            {
                var conferenceRoomService = SetupChildContext(child, room.OrganizationId);
                if (conferenceRoomService.StartMeetingFromClient(room, uniqueId, signature))
                {
                    return "Meeting started";
                }
                else
                {
                    return "Invalid link - please use the device on the outside of the room";
                }
            }
        }

        /// <summary>
        /// Marks a meeting as cancelled
        /// </summary>
        /// <param name="roomId">The address of the room</param>
        /// <param name="signature">The signature of the uniqueId - indicating it's allowed to do this</param>
        /// <param name="uniqueId">The unique ID of the meeting</param>
        [Route("{roomId}/meeting/abandonFromClient", Name = "CancelFromClient")]
        public string GetCancelMeeting(string roomId, string uniqueId, string signature)
        {
            var room = _roomRepository.GetRoomInfo(roomId);
            using (var child = _container.CreateChildContainer())
            {
                var conferenceRoomService = SetupChildContext(child, room.OrganizationId);
                if (conferenceRoomService.CancelMeetingFromClient(room, uniqueId, signature))
                {
                    return "Meeting abandoned";
                }
                else
                {
                    return "Invalid link - please use the device on the outside of the room";
                }
            }
        }

        private static IConferenceRoomService SetupChildContext(IIOCContainer child, string organizationId)
        {
            child.RegisterInstance<IContextService>(new CustomOrganizationContextService(organizationId,
                child.Resolve<ITokenProvider>(), child.Resolve<ITokenService>(), child.Resolve<IDeviceRepository>(),
                child.Resolve<IOrganizationRepository>()));
            var conferenceRoomService = child.Resolve<IConferenceRoomService>();
            return conferenceRoomService;
        }

        private class CustomOrganizationContextService : ContextService
        {
            private readonly string _organizationId;

            public CustomOrganizationContextService(string organizationId, ITokenProvider tokenProvider, ITokenService tokenService, IDeviceRepository deviceRepository, IOrganizationRepository organizationRepository) : base(tokenProvider, tokenService, deviceRepository, organizationRepository)
            {
                _organizationId = organizationId;
            }

            protected override string TokenOrganizationId => _organizationId;
        }
    }
}
