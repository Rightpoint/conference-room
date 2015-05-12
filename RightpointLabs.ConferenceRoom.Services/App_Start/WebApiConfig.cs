using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Web.Http;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            InitLogging();

            // Web API configuration and services
            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            UnityConfig.RegisterComponents();


            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }

        private static void InitLogging()
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
