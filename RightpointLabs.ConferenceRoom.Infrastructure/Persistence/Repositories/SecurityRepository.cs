using System.Linq;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class SecurityRepository : EntityRepository<SecurityRequest>, ISecurityRepository
    {
        public SecurityRepository(SecurityRequestCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public SecurityStatus GetSecurityRights(string roomAddress, string securityKey)
        {
            return
                Collection.Find(Query<SecurityRequest>.Where(i => i.RoomId == roomAddress && i.Key == securityKey))
                    .Select(i => (SecurityStatus?) i.Status)
                    .Max() ?? SecurityStatus.None;
        }

        public void RequestAccess(string roomAddress, string securityKey)
        {
            base.Add(new SecurityRequest()
            {
                RoomId = roomAddress,
                Key = securityKey,
            });
        }
    }
}