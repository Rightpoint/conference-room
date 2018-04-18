using System;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Mvc;
using log4net;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Practices.Unity;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Domain;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Models;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeEWS;
using RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest;
using RightpointLabs.ConferenceRoom.Web.SignalR;
using Unity.WebApi;

using AzureTable = RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable;
using Microsoft.Practices.Unity.InterceptionExtension;
using System.Web.Http.ExceptionHandling;

namespace RightpointLabs.ConferenceRoom.Web
{
    public static class UnityConfig
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void RegisterComponents()
        {
            var container = new UnityContainer();
            container.AddNewExtension<Interception>();

            container.RegisterType<ExchangeConferenceRoomServiceConfiguration>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Exchange",
                            _ => new ExchangeConferenceRoomServiceConfiguration() {
                                IgnoreFree = (bool)_.IgnoreFree.Value,
                                ImpersonateForAllCalls = (bool)_.ImpersonateForAllCalls.Value,
                                UseChangeNotification = (bool)_.UseChangeNotification.Value,
                                EmailDomains = ((JArray)_.EmailDomains).Select(i => i.Value<string>()).ToArray(),
                            })));

            container.RegisterType<Func<ExchangeService>>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Exchange",
                            _ => ExchangeConferenceRoomService.GetExchangeServiceBuilder((string)_.Username.Value, (string)_.Password.Value, (string)_.ServiceUrl.Value))));

            container.RegisterType<ISmsMessagingService>(new HierarchicalLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "Plivo",
                            _ => new SmsMessagingService((string)_.AuthId.Value, (string)_.AuthToken.Value, (string)_.From.Value))));

            container.RegisterType<IGdoService>(new ContainerControlledLifetimeManager(),
                new InjectionFactory(
                    c =>
                        CreateOrganizationalService(c, "GDO",
                            _ => new GdoService(new Uri((string)_.BaseUrl.Value), (string)_.ApiKey.Value, (string)_.Username.Value, (string)_.Password.Value))));

            container.RegisterType<IBroadcastService, SignalrBroadcastService>(new HierarchicalLifetimeManager());
            container.RegisterType<IConnectionManager>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => GlobalHost.ConnectionManager));
            container.RegisterType<IDateTimeService>(new ContainerControlledLifetimeManager(), new InjectionFactory(c => new DateTimeService(TimeSpan.FromHours(0))));

            container.RegisterType<HttpContextBase>(new HierarchicalLifetimeManager(), new InjectionFactory(c => new HttpContextWrapper(HttpContext.Current)));
            container.RegisterType<HttpRequestBase>(new HierarchicalLifetimeManager(), new InjectionFactory(c => c.Resolve<HttpContextBase>().Request));

            container.RegisterType<ISmsAddressLookupService, SmsAddressLookupService>(new HierarchicalLifetimeManager());
            container.RegisterType<ISignatureService, SignatureService>(new ContainerControlledLifetimeManager());


            container.RegisterType<IMeetingCacheService, MeetingCacheService>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<ISimpleTimedCache, SimpleTimedCache>(new ContainerControlledLifetimeManager()); // singleton cache
            container.RegisterType<IContextService, ContextService>(new HierarchicalLifetimeManager());
            container.RegisterType<IConferenceRoomDiscoveryService, ExchangeConferenceRoomDiscoveryService>(new HierarchicalLifetimeManager());
            container.RegisterType<ITokenService, TokenService>(new HierarchicalLifetimeManager(), new InjectionFactory(c =>
                new TokenService(
                    ConfigurationManager.AppSettings["TokenIssuer"],
                    ConfigurationManager.AppSettings["TokenAudience"],
                    ConfigurationManager.AppSettings["TokenKey"],
                    c.Resolve<OpenIdV1ConnectConfigurationService>(),
                    c.Resolve<OpenIdV2ConnectConfigurationService>())));
            container.RegisterType<OpenIdV1ConnectConfigurationService>(new ContainerControlledLifetimeManager());
            container.RegisterType<OpenIdV2ConnectConfigurationService>(new ContainerControlledLifetimeManager());

            container.RegisterType<CloudTableClient>(new ContainerControlledLifetimeManager(), new InjectionFactory(c =>
                CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureStorage"]?.ConnectionString).CreateCloudTableClient()
            ));

            container.RegisterType<CachingBehavior>(new ContainerControlledLifetimeManager());
            container.RegisterType<IDeviceStatusRepository, AzureTable.DeviceStatusRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IMeetingRepository, AzureTable.MeetingRepository>(new HierarchicalLifetimeManager());
            container.RegisterType<IRoomMetadataRepository, AzureTable.RoomMetadataRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IFloorRepository, AzureTable.FloorRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IBuildingRepository, AzureTable.BuildingRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IDeviceRepository, AzureTable.DeviceRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IOrganizationRepository, AzureTable.OrganizationRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IOrganizationServiceConfigurationRepository, AzureTable.OrganizationServiceConfigurationRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());
            container.RegisterType<IGlobalAdministratorRepository, AzureTable.GlobalAdministratorRepository>(new HierarchicalLifetimeManager(), new Interceptor<InterfaceInterceptor>(), new InterceptionBehavior<CachingBehavior>());

            container.RegisterType<IIOCContainer, UnityIOCContainer>(new TransientLifetimeManager(), new InjectionFactory(c => new UnityIOCContainer(c, false)));
            container.RegisterType<ITokenProvider, HttpTokenProvider>(new HierarchicalLifetimeManager());
            container.RegisterType<ExchangeRestWrapperFactoryCache, ExchangeRestWrapperFactoryCache>(new ContainerControlledLifetimeManager());

            container.RegisterType<IExchangeServiceManager, ExchangeServiceManager>(new ContainerControlledLifetimeManager());
            container.RegisterInstance(new MeetingCacheReloaderFactory(new UnityIOCContainer(container, false)));

            container.RegisterType<IExceptionHandler, SimpleExceptionHandler>(new ContainerControlledLifetimeManager());

            if (false)
            {
                container.RegisterType<ISyncConferenceRoomService, ExchangeConferenceRoomService>(new HierarchicalLifetimeManager());
                container.RegisterType<IConferenceRoomService, SyncConferenceRoomServiceWrapper>(new HierarchicalLifetimeManager());

                // create change notifier in a child container and register as a singleton with the main container (avoids creating it's dependencies in the global container)
                var child = container.CreateChildContainer();
                var changeNotificationService = child.Resolve<ExchangeEWSChangeNotificationService>();
                container.RegisterInstance(typeof(IChangeNotificationService), changeNotificationService, new ContainerControlledLifetimeManager());
            }
            else
            {
                container.RegisterType<ExchangeRestWrapperFactoryFactory>(new ContainerControlledLifetimeManager());

                container.RegisterType<ExchangeRestWrapperFactoryFactory.ExchangeRestWrapperFactory>(new HierarchicalLifetimeManager(), new InjectionFactory(
                    c =>
                    {
                        var f = c.Resolve<ExchangeRestWrapperFactoryFactory>();
                        if (true)
                        {
                            return CreateOrganizationalService(c, "Exchange", _ =>
                                f.GetFactory(null,
                                    (string)_.TenantId.Value,
                                    (string)_.ClientId.Value,
                                    (string)_.ClientCertificate.Value,
                                    (string)_.DefaultUser.Value));
                        }
                        else
                        {
                            return CreateOrganizationalService(c, "Exchange", _ =>
                                f.GetFactory(null,
                                    (string)_.ClientId.Value,
                                    (string)_.ClientSecret.Value,
                                    (string) _.Username.Value,
                                    (string) _.Password.Value,
                                    "me"));
                        }
                    }));

                container.RegisterType<IConferenceRoomService, ExchangeRestConferenceRoomService>(new HierarchicalLifetimeManager());

                container.RegisterType<ExchangeRestWrapper>(new HierarchicalLifetimeManager(),
                    new InjectionFactory(
                        c =>
                        {
                            var svc = c.Resolve<ExchangeRestWrapperFactoryFactory.ExchangeRestWrapperFactory>();
                            return System.Threading.Tasks.Task.Run(async () => await svc.CreateExchange()).Result;
                        }));
                container.RegisterType<GraphRestWrapper>(new HierarchicalLifetimeManager(),
                    new InjectionFactory(
                        c =>
                        {
                            var svc = c.Resolve<ExchangeRestWrapperFactoryFactory.ExchangeRestWrapperFactory>();
                            return System.Threading.Tasks.Task.Run(async () => await svc.CreateGraph()).Result;
                        }));

                // create change notifier in a child container and register as a singleton with the main container (avoids creating it's dependencies in the global container)
                var child = container.CreateChildContainer();
                //var changeNotificationService = child.Resolve<ExchangeRestChangeNotificationService>();
                //container.RegisterInstance(typeof(IExchangeRestChangeNotificationService), changeNotificationService, new ContainerControlledLifetimeManager());
                child.RegisterType<ExchangePushChangeNotificationService>(new TransientLifetimeManager(),
                    new InjectionFactory(
                        c =>
                        {
                            return new ExchangePushChangeNotificationService(c.Resolve<IBroadcastService>(),
                                c.Resolve<IMeetingCacheService>(), 
                                c.Resolve<IIOCContainer>(),
                                ConfigurationManager.AppSettings["ServiceBusConnectionString"],
                                ConfigurationManager.AppSettings["ServiceBusConnectionTopic"],
                                ConfigurationManager.AppSettings["ServiceBusConnectionSubscription"]);
                        }));
                var changeNotificationService = child.Resolve<ExchangePushChangeNotificationService>();
                new System.Threading.Thread(changeNotificationService.RecieveMessages).Start();
                container.RegisterInstance(typeof(IExchangeRestChangeNotificationService), changeNotificationService, new ContainerControlledLifetimeManager());
            }

            // initialize all repositories in a child container (ie. create tables/etc.)
            {
                using (var c = container.CreateChildContainer())
                {
                    foreach (var r in new IRepository[]
                    {
                        c.Resolve<IMeetingRepository>(), c.Resolve<IRoomMetadataRepository>(),
                        c.Resolve<IFloorRepository>(), c.Resolve<IBuildingRepository>(), c.Resolve<IDeviceRepository>(),
                        c.Resolve<IOrganizationRepository>(), c.Resolve<IOrganizationServiceConfigurationRepository>(),
                        c.Resolve<IGlobalAdministratorRepository>(),
                    })
                    {
                        r.Init();
                    }
                }
            }
            
            // register all your components with the container here
            // it is NOT necessary to register your controllers
            
            // e.g. container.RegisterType<ITestService, TestService>();
            
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
            DependencyResolver.SetResolver(new Unity.Mvc5.UnityDependencyResolver(container));
        }

        private class UnityIOCContainer : IIOCContainer
        {
            private readonly IUnityContainer _unityContainer;
            private readonly bool _owned;

            public UnityIOCContainer(IUnityContainer unityContainer, bool owned)
            {
                _unityContainer = unityContainer;
                _owned = owned;
            }

            public IIOCContainer CreateChildContainer()
            {
                return new UnityIOCContainer(_unityContainer.CreateChildContainer(), true);
            }

            public object Resolve(Type type)
            {
                return _unityContainer.Resolve(type);
            }

            public T Resolve<T>()
            {
                return (T)Resolve(typeof(T));
            }

            public void Dispose()
            {
                if (_owned)
                {
                    _unityContainer.Dispose();
                }
            }

            public void RegisterInstance<TI>(TI instance)
            {
                _unityContainer.RegisterInstance(typeof(TI), instance, new HierarchicalLifetimeManager());
            }
        }

        private static T CreateOrganizationalService<T>(IUnityContainer container, string serviceName, Func<dynamic, T> builder)
        {
            var org = container.Resolve<IContextService>().CurrentOrganization;
            if (null == org)
            {
                log.WarnFormat("Unable to load configuration for {0}, no current organization", serviceName);
                return default(T);
            }
            var config = container.Resolve<IOrganizationServiceConfigurationRepository>().Get(org.Id, serviceName);
            if (null == config)
            {
                log.WarnFormat("Unable to load configuration for {0}, no configuration found for current organization ({1})", serviceName, org.Id);
                return default(T);
            }
            try
            {
                return builder(config.Parameters);
            }
            catch (Exception ex)
            {
                log.WarnFormat("Failed to create {0} for {1}: {2}", serviceName, org.Id, ex);
                return default(T);
            }
        }

        private class SignalrBroadcastService : IBroadcastService
        {
            private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            private readonly IConnectionManager _connectionManager;

            public SignalrBroadcastService(IConnectionManager connectionManager)
            {
                _connectionManager = connectionManager;
            }

            public void BroadcastUpdate(OrganizationEntity org, IRoom room)
            {
                var context = _connectionManager.GetHubContext<UpdateHub>();
                var groupName = UpdateHub.GetGroupName(org, room);
                log.DebugFormat("Broadcasting update to {0} for {1}", groupName, room.Id);

                context.Clients.Group(groupName).Update(room.Id);
            }

            public void BroadcastDeviceChange(OrganizationEntity org, DeviceEntity device)
            {
                var context = _connectionManager.GetHubContext<UpdateHub>();
                var groupName = UpdateHub.GetGroupName(org, device);
                log.DebugFormat("Broadcasting update to {0} for {1}", groupName, device.Id);

                context.Clients.Group(groupName).DeviceChanged(device.Id);
            }

            public void BroadcastRefresh(OrganizationEntity org, DeviceEntity device = null)
            {
                var context = _connectionManager.GetHubContext<UpdateHub>();
                var groupName = UpdateHub.GetGroupName(org, device) ?? UpdateHub.GetGroupName(org);
                log.DebugFormat("Broadcasting update to {0} for {1}", groupName, device?.Id);

                context.Clients.Group(groupName).RefreshAll();
            }
        }
    }
}