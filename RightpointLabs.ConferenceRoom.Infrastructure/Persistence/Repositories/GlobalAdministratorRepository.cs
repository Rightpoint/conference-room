﻿using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver.Builders;
using RightpointLabs.ConferenceRoom.Domain.Repositories;
using RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Collections;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Persistence.Repositories
{
    public class GlobalAdministratorRepository : EntityRepository<GlobalAdministratorEntity>, IGlobalAdministratorRepository
    {
        public GlobalAdministratorRepository(GlobalAdministratorEntityCollectionDefinition collectionDefinition)
            : base(collectionDefinition)
        {
        }

        public bool IsGlobalAdmin(string username)
        {
            return Queryable.Any(e => e.Username == username);
        }
    }
}