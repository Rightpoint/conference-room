using System;
using System.Threading.Tasks;

namespace RightpointLabs.ConferenceRoom.Domain.Services
{
    public interface ISimpleTimedCache
    {
        Task<T> GetCachedValue<T>(string key, TimeSpan ageLimit, Func<Task<T>> loader);
    }
}