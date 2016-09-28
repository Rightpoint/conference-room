using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    [RoutePrefix("api/devices")]
    public class DeviceController : ApiController
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ITokenService _tokenService;

        public DeviceController(IOrganizationRepository organizationRepository, IDeviceRepository deviceRepository, ITokenService tokenService)
        {
            _organizationRepository = organizationRepository;
            _deviceRepository = deviceRepository;
            _tokenService = tokenService;
        }

        [Route("create")]
        public object PostCreate(string organizationId, string joinKey)
        {
            var org = _organizationRepository.Get(organizationId);

            if (org?.JoinKey != joinKey)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            var device = _deviceRepository.Create(new DeviceEntity()
            {
                OrganizationId = org.Id
            });

            var token = CreateToken(device);

            return token;
        }

        private string CreateToken(DeviceEntity device)
        {
            return _tokenService.CreateDeviceToken(device.Id);
        }
    }
}
