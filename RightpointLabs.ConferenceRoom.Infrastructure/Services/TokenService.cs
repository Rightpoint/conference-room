using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly string _algorithm;
        private readonly SecurityKey _signingKey;

        private static readonly string ClaimKeyDeviceId = "deviceid";
        private static readonly string ClaimKeyOrganizationId = "organizationid";
        private static readonly string ClaimKeyUserId = "userid";

        public TokenService(string issuer, string signingKey, string algorithm)
        {
            _issuer = issuer;
            _algorithm = algorithm;
            _signingKey = new JsonWebKey(signingKey);
        }

        public JwtSecurityToken ValidateToken(string rawToken)
        {
            SecurityToken token;
            var principal = new JwtSecurityTokenHandler().ValidateToken(rawToken, new TokenValidationParameters()
            {
                ValidIssuer = _issuer,
                IssuerSigningKey = new JsonWebKey()
            }, out token);
            var realToken = token as JwtSecurityToken;

            return realToken;
        }

        public string CreateDeviceToken(string deviceId)
        {
            return CreateToken(TimeSpan.FromDays(365 * 1000), new Claim(ClaimKeyDeviceId, deviceId));
        }

        public string CreateUserToken(string userId, string organizationId)
        {
            return CreateToken(TimeSpan.FromHours(4), new Claim(ClaimKeyUserId, userId), new Claim(ClaimKeyOrganizationId, organizationId));
        }

        public string GetDeviceId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyDeviceId);
        }

        public string GetUserId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyUserId);
        }

        public string GetOrganizationId(JwtSecurityToken token)
        {
            return GetClaimValueByType(token, ClaimKeyOrganizationId);
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
                Issuer = _issuer,
                IssuedAt = DateTime.UtcNow,
                NotBefore = DateTime.UtcNow,
                Expires = DateTime.UtcNow.Add(lifetime),
                SigningCredentials = new SigningCredentials(_signingKey, _algorithm),
            };

            var handler = new JwtSecurityTokenHandler();
            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}
