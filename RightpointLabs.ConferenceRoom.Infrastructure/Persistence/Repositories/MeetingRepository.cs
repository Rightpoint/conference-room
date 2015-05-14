using System.Linq;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class MeetingRepository : EntityRepository<MeetingInfoValues>, IMeetingRepository
    {
        public MeetingRepository(MeetingInfoValuesCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public MeetingInfo GetMeetingInfo(string uniqueId)
        {
            return Collection.FindOne(Query<MeetingInfoValues>.Where(i => i.Id == uniqueId));
        }

        public MeetingInfo[] GetMeetingInfo(string[] uniqueIds)
        {
            return Collection.Find(Query<MeetingInfoValues>.In(i => i.Id, uniqueIds)).Cast<MeetingInfo>().ToArray();
        }
    }
}