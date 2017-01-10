using System;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Web;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ContextService : IContextService
    {
        private readonly ITokenProvider _tokenProvider;
        private readonly ITokenService _tokenService;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IOrganizationRepository _organizationRepository;

        private readonly Lazy<JwtSecurityToken> _token;
        private readonly Lazy<DeviceEntity> _device;
        private readonly Lazy<OrganizationEntity> _organization;

        public ContextService(ITokenProvider tokenProvider, ITokenService tokenService, IDeviceRepository deviceRepository, IOrganizationRepository organizationRepository)
        {
            _tokenProvider = tokenProvider;
            _tokenService = tokenService;
            _deviceRepository = deviceRepository;
            _organizationRepository = organizationRepository;
            _token = new Lazy<JwtSecurityToken>(ValidateToken);
            _device = new Lazy<DeviceEntity>(GetDevice);
            _organization = new Lazy<OrganizationEntity>(GetOrganization);
        }

        private JwtSecurityToken ValidateToken()
        {
            var rawToken = _tokenProvider.GetToken();
            if (null == rawToken)
            {
                throw new AccessDeniedException("No token supplied", null);
            }

            var token = _tokenService.ValidateToken(rawToken);
            if (null == token)
            {
                throw new AccessDeniedException("Invalid token supplied", null);
            }

            return token;
        }

        private DeviceEntity GetDevice()
        {
            var id = TokenDeviceId;
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }
            return _deviceRepository.Get(id);
        }

        private OrganizationEntity GetOrganization()
        {
            var id = TokenOrganizationId;
            if (string.IsNullOrEmpty(id))
            {
                id = CurrentDevice?.OrganizationId;
            }
            if (string.IsNullOrEmpty(id))
            {
                var username = this.UserId;
                if (!string.IsNullOrEmpty(username))
                {
                    var domain = username.Split('@').Last();
                    var org = _organizationRepository.GetByUserDomain(domain);
                    id = org?.Id;
                }
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

        protected string TokenDeviceId => _tokenService.GetDeviceId(_token.Value);

        protected virtual string TokenOrganizationId => _tokenService.GetOrganizationId(_token.Value);

        public string UserId => _tokenService.GetUserId(_token.Value);

        public DeviceEntity CurrentDevice => _device.Value;

        public OrganizationEntity CurrentOrganization => _organization.Value;
    }
}
