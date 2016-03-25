using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IGdoService
    {
        Task<string> GetStatus(string deviceId);
        Task Open(string deviceId);
        Task Close(string deviceId);
    }
}