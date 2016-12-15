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
    public class GlobalAdministratorRepository : TableRepository<GlobalAdministratorEntity>, IGlobalAdministratorRepository
    {
        public GlobalAdministratorRepository(CloudTableClient client)
            : base(client)
        {
        }

        public bool IsGlobalAdmin(string username)
        {
            return this.GetById(username) != null;
        }

        public void EnsureRecordExists()
        {
            if (!this.GetAll().Any())
            {
                this.Insert(new GlobalAdministratorEntity() { Username = "REPLACE THIS WITH YOUR USERNAME" });
            }
        }

        protected override string GetRowKey(GlobalAdministratorEntity entity)
        {
            return entity.Username;
        }
    }
}