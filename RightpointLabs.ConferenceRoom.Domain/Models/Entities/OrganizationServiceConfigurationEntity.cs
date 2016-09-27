using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class OrganizationServiceConfigurationEntity : Entity
    {
        public string OrganizationId { get; set; }
        public string ServiceName { get; set; }
        public JObject Parameters { get; set; }
    }
}