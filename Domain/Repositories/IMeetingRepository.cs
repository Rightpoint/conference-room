using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IMeetingRepository : IRepository
    {
        MeetingEntity GetMeetingInfo(string organizationId, string uniqueId);
        MeetingEntity[] GetMeetingInfo(string organizationId, string[] uniqueIds);
        void StartMeeting(string organizationId, string uniqueId);
        void CancelMeeting(string organizationId, string uniqueId);
        void EndMeeting(string organizationId, string uniqueId);
        Task<MeetingEntity> GetMeetingInfoAsync(string organizationId, string uniqueId);
        Task<MeetingEntity[]> GetMeetingInfoAsync(string organizationId, string[] uniqueIds);
    }

}