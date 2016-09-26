using System;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models
{
    public class FloorInfoValues : FloorInfo
    {
        public DateTime LastModified { get; set; }
    }
}