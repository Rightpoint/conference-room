using System.Collections.Generic;
using System.Threading.Tasks;
using RightpointLabs.ConferenceRoom.Domain.Models;
using RightpointLabs.ConferenceRoom.Domain.Models.Entities;

namespace RightpointLabs.ConferenceRoom.Domain.Repositories
{
    public interface IOrganizationRepository : IRepository
    {
        OrganizationEntity Get(string organizationId);

        OrganizationEntity GetByUserDomain(string userDomain);
        IEnumerable<OrganizationEntity> GetByAdministrator(string user);
        IEnumerable<OrganizationEntity> GetAll();
        void Save(OrganizationEntity model);
        Task<OrganizationEntity> GetAsync(string organizationId);
    }
}