using System.Configuration;
using System.Threading;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with settings
    /// </summary>
    [RoutePrefix("api/settings")]
    public class SettingsController : ApiController
    {
        /// <summary>
        /// Checks that the supplied code is correct.
        /// </summary>
        /// <param name="code">The code to check</param>
        [Route("checkCode")]
        public bool PostCheckCode(string code)
        {
            var realCode = ConfigurationManager.AppSettings["settingsSecurityCode"];
            if (string.IsNullOrEmpty(realCode) || code == realCode)
            {
                return true;
            }
            Thread.Sleep(1000);
            return false;
        }
    }
}
