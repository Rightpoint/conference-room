using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
{
    [RoutePrefix("api/devices")]
    public class DeviceController : ApiController
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ITokenService _tokenService;
        private readonly IContextService _contextService;
        private readonly IDeviceStatusRepository _deviceStatusRepository;

        public DeviceController(IOrganizationRepository organizationRepository, IDeviceRepository deviceRepository, ITokenService tokenService, IContextService contextService, IDeviceStatusRepository deviceStatusRepository)
        {
            _organizationRepository = organizationRepository;
            _deviceRepository = deviceRepository;
            _tokenService = tokenService;
            _contextService = contextService;
            _deviceStatusRepository = deviceStatusRepository;
        }

        [Route("create")]
        public HttpResponseMessage PostCreate(string organizationId, string joinKey)
        {
            var org = _organizationRepository.Get(organizationId);

            if (null == org || org.JoinKey != joinKey)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            var device = _deviceRepository.Create(new DeviceEntity()
            {
                OrganizationId = org.Id
            });

            var token = _tokenService.CreateDeviceToken(device.Id);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(token, Encoding.UTF8) };
        }


        [Route("create")]
        public HttpResponseMessage GetCreate(string organizationId, string joinKey)
        {
            return PostCreate(organizationId, joinKey);
        }


        [Route("state")]
        public HttpResponseMessage PostState(DeviceEntity.DeviceState state)
        {
            var device = _contextService.CurrentDevice;
            if(null == device)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            device.ReportedState = state;
            device.ReportedState.ServerUtcTime = DateTime.UtcNow;
            _deviceRepository.Update(device);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }

        [Route("status")]
        public HttpResponseMessage PostStatus(DeviceStatus status)
        {
            var device = _contextService.CurrentDevice;
            if (null == device)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            status.DeviceId = device.Id;
            status.OrganizationId = device.OrganizationId;
            status.StatusTimestamp = DateTime.UtcNow;
            _deviceStatusRepository.Insert(status);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
