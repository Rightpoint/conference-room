using System;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public interface IByOrganizationId
    {
        string OrganizationId { get; set; }
    }
}