using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace OrganizationService
{
    public class OrganizationRepository : TableRepository<Organization>
    {
        public OrganizationRepository(CloudTableClient client)
            : base(client)
        {
        }

        public async Task<Organization> GetByUserDomainAsync(string userDomain)
        {
            return (await this.GetAllAsync()).FirstOrDefault(i => i.UserDomains.Contains(userDomain));
        }

        public async Task<IEnumerable<Organization>> GetByAdministratorAsync(string user)
        {
            return (await this.GetAllAsync()).Where(i => i.Administrators.Contains(user));
        }
    }
}