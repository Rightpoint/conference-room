using System.Web;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class HttpTokenProvider : ITokenProvider
    {
        private readonly HttpRequestBase _request;

        public HttpTokenProvider(HttpRequestBase request)
        {
            _request = request;
        }

        public string GetToken()
        {
            var authHeaderValue = _request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authHeaderValue) || !authHeaderValue.StartsWith("Bearer ") || authHeaderValue.Length < 8)
            {
                return null;
            }

            return authHeaderValue.Substring(7);
        }
    }
}