using System;
using System.Configuration;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.IdentityModel.Protocols;
using Task = System.Threading.Tasks.Task;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class OpenIdConnectConfigurationService
    {
        private readonly string _configUrl;

        public OpenIdConnectConfigurationService(string configUrl)
        {
            _configUrl = configUrl;
        }

        private DateTime? _metadataRetrievalDateTime;
        private OpenIdConnectConfiguration _config;

        public OpenIdConnectConfiguration GetConfig()
        {
            if (null == _config || 
                string.IsNullOrEmpty(_config.Issuer) || 
                null == _config.SigningTokens ||
                _config.SigningTokens.Count == 0 ||
                null == _metadataRetrievalDateTime ||
                DateTime.UtcNow.Subtract(_metadataRetrievalDateTime.Value).TotalDays > 1)
            {
                var url = string.Format(ConfigurationManager.AppSettings["ida:AADInstance"], ConfigurationManager.AppSettings["ida:Tenant"]) + _configUrl;
                _config = Task.Run(async () => await new ConfigurationManager<OpenIdConnectConfiguration>(url).GetConfigurationAsync()).Result;
                _metadataRetrievalDateTime = DateTime.UtcNow;
            }

            return _config;
        }
    }

    public class OpenIdV1ConnectConfigurationService : OpenIdConnectConfigurationService
    {
        public OpenIdV1ConnectConfigurationService() : base("/.well-known/openid-configuration")
        {
        }
    }

    public class OpenIdV2ConnectConfigurationService : OpenIdConnectConfigurationService
    {
        public OpenIdV2ConnectConfigurationService() : base("/v2.0/.well-known/openid-configuration")
        {
        }
    }
}
