using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Models.Entities
{
    public class OrganizationEntity : Entity
    {
        public string JoinKey { get; set; }
        public string[] UserDomains { get; set; }
    }
}
