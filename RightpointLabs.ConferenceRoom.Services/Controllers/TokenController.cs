using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    [RoutePrefix("api/tokens")]
    public class TokenController : ApiController
    {
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
        }
    }
}
