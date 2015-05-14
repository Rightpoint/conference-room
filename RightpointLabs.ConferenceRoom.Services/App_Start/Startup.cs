using Microsoft.Owin;
using Owin;
using RightpointLabs.ConferenceRoom.Services;

[assembly: OwinStartup(typeof(Startup))]

namespace RightpointLabs.ConferenceRoom.Services
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
