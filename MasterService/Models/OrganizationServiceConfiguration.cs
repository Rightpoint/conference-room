using RightpointLabs.ConferenceRoom.Shared;

namespace MasterService.Models
{
    public class OrganizationServiceConfiguration : Entity, IByOrganizationId
    {
        public string OrganizationId { get; set; }
        public string ServiceName { get; set; }
        public dynamic Parameters { get; set; }
    }
}