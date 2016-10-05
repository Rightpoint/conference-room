using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Web.Controllers
{
    [RoutePrefix("api/tokens")]
    public class TokenController : ApiController
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ITokenService _tokenService;
        private readonly IContextService _contextService;

        public TokenController(IOrganizationRepository organizationRepository, ITokenService tokenService, IContextService contextService)
        {
            _organizationRepository = organizationRepository;
            _tokenService = tokenService;
            _contextService = contextService;
        }

        [Route("get")]
        public HttpResponseMessage PostGet()
        {
            var cp = ClaimsPrincipal.Current;
            if (null == cp)
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("Not authenticated") };
            }

            var username = cp.Identities.FirstOrDefault(_ => _.IsAuthenticated && _.AuthenticationType == "AzureAdAuthCookie")?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("No username available") };
            }

            var domain = username.Split('@').Last();
            var org = _organizationRepository.GetByUserDomain(domain);
            if (null == cp)
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new StringContent("Domain not part of any organization")};
            }

            var token = _tokenService.CreateUserToken(username, org.Id);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(token, Encoding.UTF8) };
        }

        [Route("get")]
        public HttpResponseMessage GetGet()
        {
            return PostGet();
        }

        [Route("info")]
        public object GetInfo()
        {
            var org = _contextService.CurrentOrganization;
            var device = _contextService.CurrentDevice;
            var userId = _contextService.UserId;

            return new
            {
                organization = org?.Id,
                device = device?.Id,
                building = device?.BuildingId,
                controlledRooms = device?.ControlledRoomAddresses,
                user = userId,
            };
        }
    }
}
