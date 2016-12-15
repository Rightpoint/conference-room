using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Wrappers;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.Mongo
{
    public class MeetingRepository : EntityRepository<MeetingEntity>, IMeetingRepository
    {
        public MeetingRepository(MeetingEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public MeetingEntity GetMeetingInfo(string organizationId, string uniqueId)
        {
            return Collection.FindOne(Query<MeetingEntity>.Where(i => i.UniqueId == uniqueId));
        }

        public MeetingEntity[] GetMeetingInfo(string organizationId, string[] uniqueIds)
        {
            return Collection.Find(Query<MeetingEntity>.In(i => i.UniqueId, uniqueIds)).Cast<MeetingEntity>().ToArray();
        }

        public void StartMeeting(string organizationId, string uniqueId)
        {
            var update = Update<MeetingEntity>
                .Set(i => i.IsStarted, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingEntity>.Where(i => i.UniqueId == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }

        public void CancelMeeting(string organizationId, string uniqueId)
        {
            var update = Update<MeetingEntity>
                .Set(i => i.IsCancelled, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingEntity>.Where(i => i.UniqueId == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }

        public void EndMeeting(string organizationId, string uniqueId)
        {
            var update = Update<MeetingEntity>
                .Set(i => i.IsEndedEarly, true)
                .Set(i => i.LastModified, DateTime.Now);

            var result = Collection.Update(Query<MeetingEntity>.Where(i => i.UniqueId == uniqueId), update, UpdateFlags.Upsert, WriteConcern.Acknowledged);
            if (result.DocumentsAffected != 1)
            {
                throw new Exception(string.Format("Expected to affect {0} documents, but affected {1}", 1, result.DocumentsAffected));
            }
        }
    }
}