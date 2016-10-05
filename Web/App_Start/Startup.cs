using System.Configuration;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using RightpointLabs.ConferenceRoom.Web;
using RightpointLabs.ConferenceRoom.Web.Auth;

[assembly: OwinStartup(typeof(Startup))]

namespace RightpointLabs.ConferenceRoom.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
            app.SetDefaultSignInAsAuthenticationType("AzureAdAuthCookie");
            app.UseCookieAuthentication(new CookieAuthenticationOptions() { AuthenticationType = "AzureAdAuthCookie" });

            // requests to /azure-ad-auth will 302 to Azure AD, back to /azure-ad-auth, which will 302 to /
            // which means our SPA can hit /api/tokens/get, and if it gets a 401, redirect to /azure-ad-auth to prompt the user for creds, which will then lead back to the SPA,
            //   where /api/tokens/get should work.
            var authOptions = new OpenIdConnectAuthenticationOptions
            {
                ClientId = ConfigurationManager.AppSettings["ida:ClientId"],
                Authority = string.Format(ConfigurationManager.AppSettings["ida:AADInstance"], ConfigurationManager.AppSettings["ida:Tenant"]),
                PostLogoutRedirectUri = ConfigurationManager.AppSettings["ida:Audience"],
            };
            //app.UseOpenIdConnectAuthentication(authOptions);
            app.Use(typeof(CustomOpenIdConnectAuthenticationMiddleware), app, authOptions);
        }
    }
}
