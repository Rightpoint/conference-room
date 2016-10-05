using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Models
{
    public class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
