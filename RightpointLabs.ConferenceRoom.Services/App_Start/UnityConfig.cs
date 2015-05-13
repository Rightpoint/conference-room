using System;
using System.Configuration;
using System.Reflection;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;
using Unity.WebApi;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            var serviceBuilder =
                ExchangeConferenceRoomService.GetExchangeServiceBuilder(
                    ConfigurationManager.AppSettings["username"],
                    ConfigurationManager.AppSettings["password"],
                    ConfigurationManager.AppSettings["serviceUrl"]);

            container.RegisterType<ExchangeService>(new InjectionFactory(c => serviceBuilder()));
            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}