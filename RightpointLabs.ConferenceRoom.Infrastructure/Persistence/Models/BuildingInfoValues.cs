using System;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models
{
    public class BuildingInfoValues : BuildingInfo
    {
        public DateTime LastModified { get; set; }
    }
}