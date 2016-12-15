using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class OrganizationRepository : TableRepository<OrganizationEntity>, IOrganizationRepository
    {
        public OrganizationRepository(CloudTableClient client)
            : base(client)
        {
        }

        public OrganizationEntity Get(string organizationId)
        {
            return this.GetById(organizationId);
        }

        public OrganizationEntity GetByUserDomain(string userDomain)
        {
            return this.GetAll().FirstOrDefault(i => i.UserDomains.Contains(userDomain));
        }

        public IEnumerable<OrganizationEntity> GetByAdministrator(string user)
        {
            return this.GetAll().Where(i => i.Administrators.Contains(user));
        }

        public void Save(OrganizationEntity organization)
        {
            Upsert(organization);
        }
    }
}