using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public interface ITokenProvider
    {
        string GetToken();
    }
}
