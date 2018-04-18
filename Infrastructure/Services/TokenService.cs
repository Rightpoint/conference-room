using System;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Services;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly OpenIdV1ConnectConfigurationService _configurationServiceV1;
        private readonly OpenIdV2ConnectConfigurationService _configurationServiceV2;
        private readonly SecurityKey _signingKey;

        private static readonly string ClaimKeyDeviceId = "deviceid";
        private static readonly string ClaimKeyOrganizationId = "organizationid";
        private static readonly string ClaimKeyUserId = "userid";
        private static readonly string ClaimKeyStyle = "style";
        private static readonly string AzureAdClaimKeyUserId = "upn";
        private static readonly string AzureAdClaimKeyEmail = "email";
        private static readonly string AzureAdClaimKeyPreferredUsername = "preferred_username";

        public TokenService(string issuer, string audience, string signingKey, OpenIdV1ConnectConfigurationService configurationServiceV1, OpenIdV2ConnectConfigurationService configurationServiceV2)
        {
            _issuer = issuer;
            _audience = audience;
            _configurationServiceV1 = configurationServiceV1;
            _configurationServiceV2 = configurationServiceV2;
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(signingKey));
            _signingKey = new RsaSecurityKey(rsa);;
        }

        public JwtSecurityToken ValidateToken(string rawToken)
        {
            return TryValidateToken(rawToken, GetLocalTokenValidationParameters(), false) ??
                   TryValidateToken(rawToken, GetAzureADV1TokenValidationParameters(), false) ??
                   TryValidateToken(rawToken, GetAzureADV2TokenValidationParameters(), true);
        }

        private JwtSecurityToken TryValidateToken(string rawToken, TokenValidationParameters parameters, bool throwOnError)
        {
            SecurityToken token;
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(rawToken, parameters, out token);
                var realToken = token as JwtSecurityToken;

                return realToken;
            }
            catch (Exception ex)
            {
                if(ex is SecurityTokenException stex)
                {
                    if (stex.Message.StartsWith("IDX10223"))
                    {
                        // lifetime expired
                        throw new AccessDeniedException(ex.Message, ex);
                    }
                }
                if (throwOnError)
                {
                    throw;
                }
                return null;
            }
        }

        private TokenValidationParameters GetLocalTokenValidationParameters()
        {
            return new TokenValidationParameters()
            {
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _signingKey,
            };
        }

        private TokenValidationParameters GetAzureADV1TokenValidationParameters()
        {
            var config = _configurationServiceV1.GetConfig();
            return new TokenValidationParameters()
            {
                ValidIssuer = config.Issuer,
                IssuerSigningTokens = config.SigningTokens,
                ValidateAudience = false,
            };
        }

        private TokenValidationParameters GetAzureADV2TokenValidationParameters()
        {
            var config = _configurationServiceV2.GetConfig();
            return new TokenValidationParameters()
            {
                ValidIssuer = config.Issuer,
                IssuerSigningTokens = config.SigningTokens,
                ValidateAudience = false,
            };
        }

        public string CreateDeviceToken(string deviceId)
        {
            return CreateToken(TimeSpan.FromDays(365 * 20), new Claim(ClaimKeyDeviceId, deviceId));
        }

        public string CreateUserToken(string userId, string organizationId)
        {
            return CreateToken(TimeSpan.FromHours(4), new Claim(ClaimKeyUserId, userId), new Claim(ClaimKeyOrganizationId, organizationId));
        }

        public string CreateLongTermUserToken(string userId, string organizationId)
        {
            return CreateToken(TimeSpan.FromDays(365), new Claim(ClaimKeyUserId, userId), new Claim(ClaimKeyOrganizationId, organizationId), new Claim(ClaimKeyStyle, ((int)TokenStyle.LongTerm).ToString()));
        }

        public string GetDeviceId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyDeviceId);
        }

        public string GetUserId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyUserId) ??
                   GetClaimValueByType(token, AzureAdClaimKeyUserId) ??
                   GetClaimValueByType(token, AzureAdClaimKeyEmail) ??
                   GetClaimValueByType(token, AzureAdClaimKeyPreferredUsername);
        }

        public string GetOrganizationId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyOrganizationId);
        }

        public TokenStyle GetStyle(JwtSecurityToken token)
        {
            var value = GetClaimValueByType(token, ClaimKeyStyle);
            return (TokenStyle?) (string.IsNullOrEmpty(value) ? (int?) null : int.Parse(value)) ?? TokenStyle.Default;
        }

        private string GetClaimValueByType(JwtSecurityToken token, string claimType)
        {
            return token.Claims.SingleOrDefault(_ => _.Type == claimType)?.Value;
        }

        private string CreateToken(TimeSpan lifetime, params Claim[] claims)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                TokenIssuerName = _issuer,
                AppliesToAddress = _audience,
                Lifetime = new Lifetime(DateTime.UtcNow, DateTime.UtcNow.Add(lifetime)),
                //Issuer = _issuer,
                //IssuedAt = DateTime.UtcNow,
                //NotBefore = DateTime.UtcNow,
                //Expires = DateTime.UtcNow.Add(lifetime),
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha1Signature, SecurityAlgorithms.Sha1Digest),
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
