using MongoDB.Driver;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories.AzureTable
{
    public class BuildingRepository : TableByOrganizationRepository<BuildingEntity>, IBuildingRepository
    {
        public BuildingRepository(CloudTableClient client)
            : base(client)
        {
        }

        public BuildingEntity Get(string buildingId)
        {
            return this.GetById(buildingId);
        }
    }
}