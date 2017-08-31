using System;
using Microsoft.Bot.Builder.FormFlow;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class RoomBaseCriteria : BaseCriteria
    {
        public enum OfficeOptions
        {
            Chicago = 1,
            Atlanta,
            Boston,
            Dallas,
            Denver,
            Detroit,
            [Describe("Los Angeles")]
            Los_Angeles,
        }

        public OfficeOptions? Office;

        public DateTime? StartTime;
        public DateTime? EndTime;

    }
}