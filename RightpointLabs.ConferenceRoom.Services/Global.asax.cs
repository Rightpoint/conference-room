using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Elmah.Contrib.WebApi;

namespace RightpointLabs.ConferenceRoom.Services
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            InitLogging();

            GlobalConfiguration.Configure(WebApiConfig.Register);
            GlobalConfiguration.Configuration.Filters.Add(new ElmahHandleErrorApiAttribute());
            AreaRegistration.RegisterAllAreas();
        }


        private void InitLogging()
        {
            // initialize log4net
            var file = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
            if (System.IO.File.Exists(file))
            {
                log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(file));
            }
        }

    }
}
