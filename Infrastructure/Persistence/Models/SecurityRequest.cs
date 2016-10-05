using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models
{
    public class SecurityRequest : Entity
    {
        public string RoomId { get; set; }
        public string Key { get; set; }
        public string ClientInfo { get; set; }
        public SecurityStatus Status { get; set; }
    }
}
