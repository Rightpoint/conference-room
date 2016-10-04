using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class OrganizationRepository : EntityRepository<OrganizationEntity>, IOrganizationRepository
    {
        public OrganizationRepository(OrganizationEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public OrganizationEntity Get(string organizationId)
        {
            return this.Collection.FindOne(Query<OrganizationEntity>.Where(i => i.Id == organizationId));
        }

        public OrganizationEntity GetByUserDomain(string userDomain)
        {
            return this.Collection.FindOne(Query<OrganizationEntity>.Where(i => i.UserDomains.Contains(userDomain)));
        }

        public IEnumerable<OrganizationEntity> GetByAdministrator(string user)
        {
            return this.Collection.Find(Query<OrganizationEntity>.Where(i => i.Administrators.Contains(user))).ToArray();
        }

        public void Save(OrganizationEntity organization)
        {
            var result = this.Collection.Update(Query<OrganizationEntity>.Where(i => i.Id == organization.Id), Update<OrganizationEntity>.Replace(organization));
            AssertAffected(result, 1);
        }
    }
}