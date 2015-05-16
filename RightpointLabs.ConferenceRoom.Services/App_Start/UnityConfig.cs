using System;
using System.Configuration;
using System.Data;
using System.Reflection;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using System.Web.Http;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;
using RightpointLabs.ConferenceRoom.Services.SignalR;
using Unity.WebApi;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
			var container = new UnityContainer();

            var connectionString =
                System.Web.Configuration.WebConfigurationManager.ConnectionStrings["Mongo"].ConnectionString;
            var database = System.Web.Configuration.WebConfigurationManager.ConnectionStrings["Mongo"].ProviderName;

            container.RegisterType<IMongoConnectionHandler, MongoConnectionHandler>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(connectionString, database));

            var serviceBuilder =
                ExchangeConferenceRoomService.GetExchangeServiceBuilder(
                    ConfigurationManager.AppSettings["username"],
                    ConfigurationManager.AppSettings["password"],
                    ConfigurationManager.AppSettings["serviceUrl"]);

            container.RegisterType<ExchangeService>(new HierarchicalLifetimeManager(), new InjectionFactory(c => serviceBuilder()));
            container.RegisterType<IBroadcastService, SignalrBroadcastService>(new HierarchicalLifetimeManager());
            container.RegisterType<IConferenceRoomService, ExchangeConferenceRoomService>(new HierarchicalLifetimeManager());
            container.RegisterType<IMeetingRepository, MeetingRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<ISecurityRepository, SecurityRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IConnectionManager>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => GlobalHost.ConnectionManager));
            container.RegisterType<IDateTimeService>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new DateTimeService(TimeSpan.FromHours(0))));
            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }

        private class SignalrBroadcastService : IBroadcastService
        {
            private readonly IConnectionManager _connectionManager;

            public SignalrBroadcastService(IConnectionManager connectionManager)
            {
                _connectionManager = connectionManager;
            }

            public void BroadcastUpdate(string roomAddress)
            {
                var context = _connectionManager.GetHubContext<UpdateHub>();
                context.Clients.All.Update(roomAddress);
            }
        }
    }
}