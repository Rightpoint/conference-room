using System;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models
{
    public class RoomInfoValues : RoomMetadata
    {
        public DateTime LastModified { get; set; }
    }
}