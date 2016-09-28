using System.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
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
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // this registers the OpenIdConnect Middleware, which has the AuthenticationHandler, which redirects all 401s via ApplyResponseChallengeAsync
            // since we only want a single URL to cause that, we'll just override the middleware to build an overriden AuthenticationHandler,
            //   which will override AuthenticateCoreAsync to add a hidden "log me in now" URL, which will redirect to "/" if not logged in,
            //   and set a 401 and whitelist that in ApplyResponseChallengeAsync (so other 401s don't trigger that).
            //   then, we'll be able to use all this auth code without it messing up the rest of our app
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ConfigurationManager.AppSettings["ida:ClientId"],
                    Authority = string.Format(ConfigurationManager.AppSettings["ida:AADInstance"], ConfigurationManager.AppSettings["ida:Tenant"]),
                    PostLogoutRedirectUri = ConfigurationManager.AppSettings["ida:Audience"],
                });
        }
    }
}
