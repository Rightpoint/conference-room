using System.Collections.Generic;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IFloorRepository : IRepository
    {
        FloorEntity Get(string floorId);
        IEnumerable<FloorEntity> GetAllByOrganization(string organizationId);
        IEnumerable<FloorEntity> GetAllByBuilding(string buildingId);
        void Update(FloorEntity value);
        Task<FloorEntity> GetAsync(string floorId);
        Task<IEnumerable<FloorEntity>> GetAllByOrganizationAsync(string organizationId);
    }
}