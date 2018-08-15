using System.Collections.Generic;
using MasterService.Models;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace MasterService.Repository
{
    public class DeviceRepository : TableByOrganizationRepository<Device>
    {
        public DeviceRepository(CloudTableClient client)
            : base(client)
        {
        }
    }
}