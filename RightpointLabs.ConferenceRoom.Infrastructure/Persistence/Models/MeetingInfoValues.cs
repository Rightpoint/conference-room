using System;
using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models
{
    public class MeetingInfoValues : MeetingInfo
    {
        public DateTime LastModified { get; set; }
    }
}