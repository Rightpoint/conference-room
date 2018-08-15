using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MasterService.Models;
using Microsoft.WindowsAzure.Storage.Table;
using RightpointLabs.ConferenceRoom.Shared.Repository;

namespace MasterService.Repository
{
    public class MeetingExtensionRepository : TableByOrganizationRepository<MeetingExtension>
    {
        public MeetingExtensionRepository(CloudTableClient client)
            : base(client)
        {
        }

        public override Task<MeetingExtension> GetByIdAsync(string organizationId, string id)
        {
            return base.GetByIdAsync(organizationId, UniqueIdToRowKey(id));
        }

        public override Task<IEnumerable<MeetingExtension>> GetByIdAsync(string organizationId, string[] id)
        {
            return base.GetByIdAsync(organizationId, id.Select(UniqueIdToRowKey).ToArray());
        }

        protected override string GetRowKey(MeetingExtension entity)
        {
            return UniqueIdToRowKey(base.GetRowKey(entity));
        }

        private string UniqueIdToRowKey(string uniqueId)
        {
            return uniqueId.Replace("/", "_").Replace("=", "-");
        }
    }
}