using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class MeetingRepository : TableByOrganizationRepository<MeetingEntity>, IMeetingRepository
    {
        public MeetingRepository(CloudTableClient client)
            : base(client)
        {
        }

        public MeetingEntity GetMeetingInfo(string organizationId, string uniqueId)
        {
            return this.GetById(organizationId, UniqueIdToRowKey(uniqueId));
        }

        public MeetingEntity[] GetMeetingInfo(string organizationId, string[] uniqueIds)
        {
            return this.GetById(organizationId, uniqueIds.Select(UniqueIdToRowKey).ToArray()).ToArray();
        }

        protected override string GetRowKey(MeetingEntity entity)
        {
            return UniqueIdToRowKey(entity.UniqueId);
        }

        private MeetingEntity GetOrCreate(string organizationId, string uniqueId)
        {
            return this.GetById(organizationId, UniqueIdToRowKey(uniqueId)) ??
                   new MeetingEntity() {OrganizationId = organizationId, UniqueId = uniqueId};
        }

        public void StartMeeting(string organizationId, string uniqueId)
        {
            var meeting = GetOrCreate(organizationId, uniqueId);
            meeting.IsStarted = true;
            meeting.LastModified = DateTime.UtcNow;
            Upsert(meeting);
        }

        public void CancelMeeting(string organizationId, string uniqueId)
        {
            var meeting = GetOrCreate(organizationId, uniqueId);
            meeting.IsCancelled = true;
            meeting.LastModified = DateTime.UtcNow;
            Upsert(meeting);
        }

        public void EndMeeting(string organizationId, string uniqueId)
        {
            var meeting = GetOrCreate(organizationId, uniqueId);
            meeting.IsEndedEarly = true;
            meeting.LastModified = DateTime.UtcNow;
            Upsert(meeting);
        }

        private string UniqueIdToRowKey(string uniqueId)
        {
            return uniqueId.Replace("/", "_").Replace("=", "-");
        }
    }
}