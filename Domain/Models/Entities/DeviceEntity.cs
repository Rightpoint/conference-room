using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class DeviceEntity : Entity
    {
        public string OrganizationId { get; set; }
        public string BuildingId { get; set; }
        public string[] ControlledRoomIds { get; set; }
    }
}
