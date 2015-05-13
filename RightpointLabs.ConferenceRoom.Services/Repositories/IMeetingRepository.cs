using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RightpointLabs.ConferenceRoom.Services.Models;

namespace RightpointLabs.ConferenceRoom.Services.Repositories
{
    public interface IMeetingRepository
    {
        MeetingInfo GetMeetingInfo(string uniqueId);
        MeetingInfo[] GetMeetingInfo(string[] uniqueIds);
    }

}