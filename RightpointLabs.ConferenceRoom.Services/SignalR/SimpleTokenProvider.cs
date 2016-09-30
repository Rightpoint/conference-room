using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Services.SignalR
{
    public class SimpleTokenProvider : ITokenProvider
    {
        private readonly string _token;

        public SimpleTokenProvider(string token)
        {
            _token = token;
        }

        public string GetToken()
        {
            return _token;
        }
    }
}