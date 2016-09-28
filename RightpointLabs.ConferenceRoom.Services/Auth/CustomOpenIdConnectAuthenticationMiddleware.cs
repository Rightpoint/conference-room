using System;
using System.Collections.Generic;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace RightpointLabs.ConferenceRoom.Services.Auth
{
    public class CustomOpenIdConnectAuthenticationMiddleware : OpenIdConnectAuthenticationMiddleware
    {
        private ILogger _logger;

        public CustomOpenIdConnectAuthenticationMiddleware(OwinMiddleware next, IAppBuilder app, OpenIdConnectAuthenticationOptions options) : base(next, app, options)
        {
            this._logger = app.CreateLogger<CustomOpenIdConnectAuthenticationMiddleware>();
        }

        protected override AuthenticationHandler<OpenIdConnectAuthenticationOptions> CreateHandler()
        {
            return new CustomOpenIdConnectAuthenticationHandler(this._logger);
        }
    }
}