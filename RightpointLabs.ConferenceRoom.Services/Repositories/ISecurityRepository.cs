using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RightpointLabs.ConferenceRoom.Services.Models;

namespace RightpointLabs.ConferenceRoom.Services.Repositories
{
    public interface ISecurityRepository
    {
        SecurityStatus GetSecurityRights(string roomAddress, string securityKey);
        void RequestAccess(string roomAddress, string securityKey);
    }
}