using log4net;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with settings
    /// </summary>
    [RoutePrefix("api/settings")]
    public class SettingsController : BaseController
    {
        private static readonly ILog __log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SettingsController()
            : base(__log)
        { }

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
