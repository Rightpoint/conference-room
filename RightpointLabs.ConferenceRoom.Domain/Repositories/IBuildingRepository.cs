﻿using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IBuildingRepository
    {
        BuildingEntity Get(string buildingId);
        void Save(string buildingId, BuildingEntity value);
    }
}