using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.OpenIdConnect;

namespace RightpointLabs.ConferenceRoom.Services.Auth
{
    public class CustomOpenIdConnectAuthenticationHandler : OpenIdConnectAuthenticationHandler
    {
        public CustomOpenIdConnectAuthenticationHandler(ILogger logger) : base(logger)
        {
        }

        public override Task<bool> InvokeAsync()
        {
            // don't think this is thread-safe, but it's not like we're likely to get calls from different URLs interchangibly - just looking to not have to set this in each environment
            this.Options.RedirectUri = new Uri(this.Request.Uri, "/azure-ad-auth-callback").AbsoluteUri;

            if ((this.Request.PathBase + this.Request.Path).ToString() == "/azure-ad-auth")
            {
                // see if we have the auth info we need - if so, we can send the user to /
                if (ClaimsPrincipal.Current.Identities.Any(_ => _.IsAuthenticated && _.AuthenticationType == "AzureAdAuthCookie"))
                {
                    this.Response.Redirect("/");
                }
                else
                {
                    this.Response.StatusCode = 401;
                }
                return Task.FromResult(true);
            }

            return base.InvokeAsync();
        }

        protected override Task ApplyResponseCoreAsync()
        {
            if ((this.Request.PathBase + this.Request.Path).ToString() == "/azure-ad-auth")
            {
                return base.ApplyResponseCoreAsync();
            }

            // not our magic URL, so we don't care
            return Task.CompletedTask;
        }
    }
}