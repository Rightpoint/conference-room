using System.Net;
using System.Threading.Tasks;

namespace RightpointLabs.BotLib.Services
{
    public abstract class WindowsAuthServiceBase : SimpleServiceBase
    {
        private readonly string _username;
        private readonly string _password;

        protected WindowsAuthServiceBase(string username, string password)
        {
            _username = username;
            _password = password;
        }

        protected override async Task<ICredentials> GetCredentials()
        {
            return new NetworkCredential(_username, _password);
        }

        protected override string GetUserKey()
        {
            return base.GetUserKey() + "_" + _username;
        }
    }
}