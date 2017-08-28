using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols;

namespace RightpointLabs.ConferenceRoom.Bot.Services
{
    public class RoomsService : BearerAuthSimpleServiceBase
    {
        protected override Uri Url => new Uri(ConfigurationManager.AppSettings["RoomNinjaApiUrl"]);

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
            var rooms = await Get<RoomStatusResult[]>($"api/room/all/status/{buildingId}");

            foreach (var room in rooms)
            {
                room.Info.SpeakableName = MakeSpeakable(room.Info.DisplayName);
            }

            return rooms;
        }

        private string MakeSpeakable(string name)
        {
            name = new Regex(@"\(.[^)]+\)").Replace(name, "");
            name = string.Join(" ", name.Split(',').Reverse());
            name = name.Replace("  ", " ").Trim();
            return name;
        }

        [Serializable]
        public class RoomStatusResult
        {
            [Serializable]
            public class RoomInfo
            {
                public string DisplayName { get; set; }
                public string SpeakableName { get; set; }
                public string FloorName { get; set; }
                public string BuildingName { get; set; }
                public int Size { get; set; }
                public List<RoomSearchCriteria.EquipmentOptions> Equipment { get; set; }
            }

            [Serializable]
            public class RoomStatus
            {
                public double FreeFor { get; set; }
                public int Status { get; set; }
                public int RoomNextFreeInSeconds { get; set; }
                public MeetingResult Current { get; set; }
                public MeetingResult[] NearTermMeetings { get; set; }
            }

            [Serializable]
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