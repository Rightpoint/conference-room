using System;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly SecurityKey _signingKey;

        private static readonly string ClaimKeyDeviceId = "deviceid";
        private static readonly string ClaimKeyOrganizationId = "organizationid";
        private static readonly string ClaimKeyUserId = "userid";

        public TokenService(string issuer, string audience, string signingKey)
        {
            _issuer = issuer;
            _audience = audience;
            var rsa = new RSACryptoServiceProvider();
            rsa.ImportCspBlob(Convert.FromBase64String(signingKey));
            _signingKey = new RsaSecurityKey(rsa);;
        }

        public JwtSecurityToken ValidateToken(string rawToken)
        {
            SecurityToken token;
            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(rawToken, new TokenValidationParameters()
                {
                    ValidIssuer = _issuer,
                    ValidAudience = _audience,
                    IssuerSigningKey = _signingKey,
                }, out token);
                var realToken = token as JwtSecurityToken;

                return realToken;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string CreateDeviceToken(string deviceId)
        {
            return CreateToken(TimeSpan.FromDays(365 * 20), new Claim(ClaimKeyDeviceId, deviceId));
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
