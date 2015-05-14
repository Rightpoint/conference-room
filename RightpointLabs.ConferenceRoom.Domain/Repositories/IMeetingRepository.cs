using RightpointLabs.ConferenceRoom.Domain.Models;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IMeetingRepository
    {
        MeetingInfo GetMeetingInfo(string uniqueId);
        MeetingInfo[] GetMeetingInfo(string[] uniqueIds);
        void StartMeeting(string uniqueId);
        void CancelMeeting(string uniqueId);
        void EndMeeting(string uniqueId);
    }

}