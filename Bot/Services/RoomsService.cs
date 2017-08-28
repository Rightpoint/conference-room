using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;

namespace RightpointLabs.ConferenceRoom.Bot.Services
{
    public class RoomsService : BearerAuthSimpleServiceBase
    {
        public static readonly string Resource = ConfigurationManager.AppSettings["RN_Resource"];
        protected override Uri Url => new Uri(ConfigurationManager.AppSettings["RN_Url"]);

        public RoomsService(string accessToken) : base(accessToken)
        {
        }

        public async Task<BuildingResult[]> GetBuildings()
        {
            return await Get<BuildingResult[]>("api/building/all");
        }

        public class BuildingResult
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public async Task<RoomStatusResult[]> GetRoomsStatusForBuilding(string buildingId)
        {
            return await Get<RoomStatusResult[]>($"api/room/all/status/{buildingId}");
        }

        public class RoomStatusResult
        {
            public class RoomInfo
            {
                public string DisplayName { get; set; }
                public string FloorName { get; set; }
                public string BuildingName { get; set; }
                public int Size { get; set; }
                public List<RoomSearchCriteria.EquipmentOptions> Equipment { get; set; }
            }

            public class RoomStatus
            {
                public double FreeFor { get; set; }
                public int Status { get; set; }
                public int RoomNextFreeInSeconds { get; set; }
                public MeetingResult Current { get; set; }
                public MeetingResult[] NearTermMeetings { get; set; }
            }

            public class MeetingResult
            {
                public DateTime Start { get; set; }
                public DateTime End { get; set; }
                public bool IsStarted { get; set; }
            }

            public string Address { get; set; }
            public RoomInfo Info { get; set; }
            public RoomStatus Status { get; set; }
        }
    }
}