using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface IMeetingCacheReloader
    {
        Task ReloadCache(string roomAddress);
    }
}