using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeRest.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ShowAs
    {
        Free,
        Busy,
        Tentative,
    }
}