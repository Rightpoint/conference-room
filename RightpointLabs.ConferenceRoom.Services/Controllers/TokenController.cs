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
    [RoutePrefix("api/tokens")]
    public class TokenController : ApiController
    {
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ITokenService _tokenService;

        public TokenController(IOrganizationRepository organizationRepository, ITokenService tokenService)
        {
            _organizationRepository = organizationRepository;
            _tokenService = tokenService;
        }

        [Route("get")]
        public object PostGet()
        {
            var token = CreateToken(this.Request);
            if (null == token)
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }
            return token;
        }

        private string CreateToken(HttpRequestMessage message)
        {
            throw new NotImplementedException();
            var username = "user@org.com";
            var domain = username.Split('@').Last();

            var org = _organizationRepository.GetByUserDomain(domain);
            return _tokenService.CreateUserToken(username, org.Id);
        }
    }
}
