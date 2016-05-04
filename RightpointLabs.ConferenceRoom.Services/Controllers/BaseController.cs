using log4net;
using RightpointLabs.ConferenceRoom.Services.Attributes;
using System.Net.Http;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services.Controllers
{
    /// <summary>
    /// Operations dealing with client log messages
    /// </summary>
    [ErrorHandler]
    public abstract class BaseController : ApiController
    {
        protected BaseController(ILog log)
        {
            this.Log = log;
        }

        protected internal ILog Log { get; private set; }

        protected string GetClientIp(HttpRequestMessage request = null)
        {
            request = request ?? Request;

            // from https://trikks.wordpress.com/2013/06/27/getting-the-client-ip-via-asp-net-web-api/
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                RemoteEndpointMessageProperty prop = (RemoteEndpointMessageProperty)request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                return null;
            }
        }
    }
}
