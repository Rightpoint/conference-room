using System;
using System.Configuration;
using System.Data;
using System.Reflection;
using System.ServiceModel.Dispatcher;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain;
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

            var connectionStrings = System.Web.Configuration.WebConfigurationManager.ConnectionStrings;
            var connectionString = connectionStrings["Mongo"].ConnectionString;
            var providerName = connectionStrings["Mongo"].ProviderName;

            container.RegisterType<IMongoConnectionHandler, MongoConnectionHandler>(
                new ContainerControlledLifetimeManager(),
                new InjectionConstructor(connectionString, providerName));

            container.RegisterType<Func<ExchangeService>>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Exchange",
                            _ => ExchangeConferenceRoomService.GetExchangeServiceBuilder(_.Username, _.Password, _.ServiceUrl))));

            container.RegisterType<IInstantMessagingService>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Exchange",
                            _ => new InstantMessagingService(_.Username, _.Password))));

            container.RegisterType<ISmsMessagingService>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Plivo",
                            _ => new SmsMessagingService(_.AuthId, _.AuthToken, _.From))));

            container.RegisterType<IGdoService>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Plivo",
                            _ => new GdoService(new Uri(_.BaseUrl), _.ApiKey, _.Username, _.Password))));

            container.RegisterType<ISmsAddressLookupService, SmsAddressLookupService>(new HierarchicalLifetimeManager());
            container.RegisterType<ISignatureService, SignatureService>(new ContainerControlledLifetimeManager());
            container.RegisterType<IBroadcastService, SignalrBroadcastService>(new HierarchicalLifetimeManager());
            container.RegisterType<IConferenceRoomService, ExchangeConferenceRoomService>(new HierarchicalLifetimeManager());
            container.RegisterType<IMeetingRepository, MeetingRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<ISecurityRepository, SecurityRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IConnectionManager>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => GlobalHost.ConnectionManager));
            container.RegisterType<IExchangeServiceManager, ExchangeServiceManager>(new ContainerControlledLifetimeManager());
            container.RegisterType<IDateTimeService>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new DateTimeService(TimeSpan.FromHours(0))));
            container.RegisterType<IMeetingCacheService, MeetingCacheService>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<ISimpleTimedCache, SimpleTimedCache>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<IRoomMetadataRepository, RoomMetadataRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IBuildingService, BuildingService>(new HierarchicalLifetimeManager());
            container.RegisterType<IBuildingRepository, BuildingRepository>(new HierarchicalLifetimeManager());

            // create change notifier in a child container and register as a singleton with the main container (avoids creating it's dependencies in the global container)
            var child = container.CreateChildContainer();
            var changeNotificationService = child.Resolve<ChangeNotificationService>();
            container.RegisterInstance(typeof(IChangeNotificationService), changeNotificationService, new ContainerControlledLifetimeManager());

            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }

        private static T CreateOrganizationalService<T>(IUnityContainer container, string serviceName, Func<dynamic, T> builder)
        {
            var org = container.Resolve<IContextService>().CurrentOrganization;
            if (null == org)
            {
                return default(T);
            }
            var config = container.Resolve<IOrganizationServiceConfigurationRepository>().Get(org.Id, serviceName);
            if (null == config)
            {
                return default(T);
            }
            return builder(config.Parameters);
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