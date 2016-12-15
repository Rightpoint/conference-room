using System;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

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
            return this.GetById(organizationId, uniqueId);
        }

        public MeetingEntity[] GetMeetingInfo(string organizationId, string[] uniqueIds)
        {
            return this.GetById(organizationId, uniqueIds).ToArray();
        }

        protected override string GetRowKey(MeetingEntity entity)
        {
            return entity.UniqueId;
        }

        private MeetingEntity GetOrCreate(string organizationId, string uniqueId)
        {
            return this.GetById(organizationId, uniqueId) ?? new MeetingEntity() {  OrganizationId = organizationId, UniqueId = uniqueId };
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
    }
}