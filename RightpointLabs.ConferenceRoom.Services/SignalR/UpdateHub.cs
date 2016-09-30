using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using log4net;
using Microsoft.AspNet.SignalR;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Services;
using RightpointLabs.ConferenceRoom.Infrastructure.Services;

namespace RightpointLabs.ConferenceRoom.Services.SignalR
{
    public class UpdateHub : Hub
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override Task OnConnected()
        {
            log.DebugFormat("Connected as {0}", this.Context.ConnectionId);
            return base.OnConnected();
        }

        public void Authenticate(string token)
        {
            using (var scope = GlobalConfiguration.Configuration.DependencyResolver.BeginScope())
            {
                var c = (IIOCContainer)scope.GetService(typeof(IIOCContainer));
                c.RegisterInstance<ITokenProvider>(new SimpleTokenProvider(token));
                var svc = c.Resolve<IContextService>();
                var org = svc.CurrentOrganization;
                var room = (svc.CurrentDevice?.ControlledRoomAddresses ?? new string[0]).FirstOrDefault();

                var groups = new[] {GetGroupName(org), GetGroupName(org, room)}.Where(_ => _ != null).ToArray();

                foreach (var group in groups)
                {
                    log.DebugFormat("Registered group {0} for {1}", group, this.Context.ConnectionId);
                    this.Groups.Add(this.Context.ConnectionId, group);
                }
            }
        }

        public void ClientActive(string token)
        {
            using (var scope = GlobalConfiguration.Configuration.DependencyResolver.BeginScope())
            {
                var c = (IIOCContainer)scope.GetService(typeof(IIOCContainer));
                c.RegisterInstance<ITokenProvider>(new SimpleTokenProvider(token));
                var svc = c.Resolve<IContextService>();
                var org = svc.CurrentOrganization;
                var rooms = svc.CurrentDevice?.ControlledRoomAddresses ?? new string[0];

                foreach (var room in rooms)
                {
                    var groupName = GetGroupName(org, room);
                    log.DebugFormat("Sending ClientActive to {0}", groupName);
                    this.Clients.Group(groupName).ClientActive(room);
                }
            }
        }

        public static string GetGroupName(OrganizationEntity org)
        {
            if (null == org)
                return null;

            return $"org_{org.Id}";

        }

        public static string GetGroupName(OrganizationEntity org, string roomAddress)
        {
            if (null == org || string.IsNullOrEmpty(roomAddress))
                return null;

            return $"org_{org.Id}_{roomAddress}";
        }
    }
}