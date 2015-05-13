using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface ISecurityRepository
    {
        SecurityStatus GetSecurityRights(string roomAddress, string securityKey);
        void RequestAccess(string roomAddress, string securityKey);
    }
}