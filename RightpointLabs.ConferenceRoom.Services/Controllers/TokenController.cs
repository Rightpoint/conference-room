using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Web;
using System.Web.Http;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;
using ClaimTypes = System.IdentityModel.Claims.ClaimTypes;

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

        [Authorize]
        [Route("login")]
        public object GetLogin()
        {
            return Redirect("/");
        }

        [Authorize]
        [Route("get")]
        public object GetGet()
        {
            var cp = ClaimsPrincipal.Current;
            var username = cp.Identities.FirstOrDefault()?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return StatusCode(HttpStatusCode.Forbidden);
            }

            var domain = username.Split('@').Last();

            var org = _organizationRepository.GetByUserDomain(domain);
            return _tokenService.CreateUserToken(username, org.Id);
        }
    }
}
