using System.Net.Http;
using System.Net.Http.Headers;

namespace RightpointLabs.ConferenceRoom.Bot.Services
{
    public abstract class BearerAuthSimpleServiceBase : SimpleServiceBase
    {
        private readonly string _accessToken;

        protected BearerAuthSimpleServiceBase(string accessToken)
        {
            _accessToken = accessToken;
        }

        protected override void AddAuthentication(HttpClient c)
        {
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }

        protected override string GetUserKey()
        {
            return base.GetUserKey() + "_" + _accessToken;
        }
    }
}