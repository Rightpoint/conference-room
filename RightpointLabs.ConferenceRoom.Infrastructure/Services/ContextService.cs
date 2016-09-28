using System;
using System.IdentityModel.Tokens;
using System.Web;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ContextService : IContextService
    {
        private readonly ITokenService _tokenService;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IOrganizationRepository _organizationRepository;

        private readonly Lazy<JwtSecurityToken> _token;
        private readonly Lazy<DeviceEntity> _device;
        private readonly Lazy<OrganizationEntity> _organization;

        public ContextService(HttpRequestBase request, ITokenService tokenService, IDeviceRepository deviceRepository, IOrganizationRepository organizationRepository)
        {
            _tokenService = tokenService;
            _deviceRepository = deviceRepository;
            _organizationRepository = organizationRepository;
            _token = new Lazy<JwtSecurityToken>(() => GetToken(request));
            _device = new Lazy<DeviceEntity>(GetDevice);
            _organization = new Lazy<OrganizationEntity>(GetOrganization);
        }

        private JwtSecurityToken GetToken(HttpRequestBase request)
        {
            var authHeaderValue = request.Headers["Authentication"];
            if (string.IsNullOrEmpty(authHeaderValue))
            {
                return null;
            }

            return _tokenService.ValidateToken(authHeaderValue);
        }

        private DeviceEntity GetDevice()
        {
            var id = DeviceId;
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return _deviceRepository.Get(id);
        }

        private OrganizationEntity GetOrganization()
        {
            var id = OrganizationId;
            if (string.IsNullOrEmpty(id))
            {
                id = CurrentDevice?.OrganizationId;
            }
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return _organizationRepository.Get(id);
        }

        public bool IsAuthenticated
        {
            get
            {
                try
                {
                    return _token.Value != null;
                }
                catch
                {
                    return false;
                }
            }
        }

        public string DeviceId => _tokenService.GetDeviceId(_token.Value);

        public string OrganizationId => _tokenService.GetOrganizationId(_token.Value);

        public string UserId => _tokenService.GetUserId(_token.Value);

        public DeviceEntity CurrentDevice => _device.Value;

        public OrganizationEntity CurrentOrganization => _organization.Value;
    }
}
