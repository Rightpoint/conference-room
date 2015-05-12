using System;
using System.Configuration;
using System.Reflection;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using System.Web.Http;
using Unity.WebApi;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            var svcUsername = ConfigurationManager.AppSettings["username"];
            var svcPassword = ConfigurationManager.AppSettings["password"];
            // if we don't get a service URL in our configuration, run auto-discovery the first time we need it
            var svcUrl = new Lazy<string>(() =>
            {
                var configValue = ConfigurationManager.AppSettings["serviceUrl"];
                if (!string.IsNullOrEmpty(configValue))
                {
                    return configValue;
                }
                var log = log4net.LogManager.GetLogger(MethodInfo.GetCurrentMethod().DeclaringType);
                log.DebugFormat("serviceUrl wasn't configured in appSettings, running auto-discovery");
                var svc = new ExchangeService(ExchangeVersion.Exchange2010);
                svc.Credentials = new WebCredentials(svcUsername, svcPassword);
                svc.AutodiscoverUrl(svcUsername, url => new Uri(url).Scheme == "https");
                log.DebugFormat("Auto-discovery complete - found URL: {0}", svc.Url);
                return svc.Url.ToString();
            });

            container.RegisterType<ExchangeService>(new InjectionFactory(c =>
                new ExchangeService(ExchangeVersion.Exchange2010)
                {
                    Credentials = new WebCredentials(svcUsername, svcPassword),
                    Url = new Uri(svcUrl.Value),
                }));
            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}