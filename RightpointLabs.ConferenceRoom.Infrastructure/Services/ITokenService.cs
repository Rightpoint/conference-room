using System.IdentityModel.Tokens.Jwt;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public interface ITokenService
    {
        JwtSecurityToken ValidateToken(string rawToken);
        string CreateDeviceToken(string deviceId);
        string CreateUserToken(string userId, string organizationId);
        string GetDeviceId(JwtSecurityToken token);
        string GetUserId(JwtSecurityToken token);
        string GetOrganizationId(JwtSecurityToken token);
    }
}