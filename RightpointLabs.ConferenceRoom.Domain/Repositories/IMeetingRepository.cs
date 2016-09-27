using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IMeetingRepository
    {
        MeetingEntity GetMeetingInfo(string uniqueId);
        MeetingEntity[] GetMeetingInfo(string[] uniqueIds);
        void StartMeeting(string uniqueId);
        void CancelMeeting(string uniqueId);
        void EndMeeting(string uniqueId);
    }

}