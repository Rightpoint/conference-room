using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.IdentityModel.Protocols;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Criteria;

namespace RightpointLabs.ConferenceRoom.Bot.Services
{
    public class RoomsService : BearerAuthSimpleServiceBase
    {
        protected override Uri Url => BaseUrl;

        public static Uri BaseUrl => new Uri(Config.GetAppSetting("RoomNinjaApiUrl"));

        public static readonly string Resource = Config.GetAppSetting("RoomNinjaResource") ?? "https://rooms.rightpoint.com/";

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

        public async Task<RoomResult[]> GetRoomsForBuilding(string buildingId)
        {
            var rooms = await Get<RoomResult[]>($"api/room/all/{buildingId}");

            foreach (var room in rooms)
            {
                room.Info.SpeakableName = MakeSpeakable(room.Info.DisplayName);
            }

            return rooms;
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

        public async Task<RoomStatusResult> GetRoomsStatus(string roomId)
        {
            var room = await Get<RoomStatusResult>($"api/room/{roomId}/status");

            room.Info.SpeakableName = MakeSpeakable(room.Info.DisplayName);

            return room;
        }

        public async Task<string> ScheduleMeeting(string roomId, bool inviteMe, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            return await Post($"api/room/{roomId}/meeting/scheduleNew", new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"inviteMe", inviteMe.ToString() },
                {"startTime", $"{startTime:O}" },
                {"endTime", $"{endTime:O}" },
            }));
        }

        private string MakeSpeakable(string name)
        {
            name = new Regex(@"\(.[^)]+\)").Replace(name, "");
            name = string.Join(" ", name.Split(',').Reverse());
            name = name.Replace("  ", " ").Trim();
            return name;
        }

        [Serializable]
        public class RoomStatusResult : RoomResult
        {
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
                public DateTimeOffset Start { get; set; }
                public DateTimeOffset End { get; set; }
                public bool IsStarted { get; set; }
                public string Organizer { get; set; }
                public string Subject { get; set; }
                public string UniqueId { get; set; }
            }

            public RoomStatus Status { get; set; }
        }

        [Serializable]
        public class RoomResult
        {
            [Serializable]
            public class RoomInfo
            {
                public string DisplayName { get; set; }
                public string SpeakableName { get; set; }
                public string FloorName { get; set; }
                public string FloorId { get; set; }
                public string Floor { get; set; }
                public string BuildingName { get; set; }
                public string BuildingId { get; set; }
                public int Size { get; set; }
                public List<RoomSearchCriteria.EquipmentOptions> Equipment { get; set; }
            }

            public string Id { get; set; }
            public string Address { get; set; }
            public RoomInfo Info { get; set; }
        }
    }

    public static class RoomServiceResultExtensions
    {
        private static readonly Regex _cleanup = new Regex("[^A-Za-z0-9 ]*", RegexOptions.Compiled);

        public static RoomsService.RoomStatusResult MatchName(this ICollection<RoomsService.RoomStatusResult> values, string name)
        {
            name = _cleanup.Replace(name, "");
            return values.FirstOrDefault(i => i.Info.SpeakableName.ToLowerInvariant() == name.ToLowerInvariant()) ??
                   values.FirstOrDefault(i =>
                       string.Join(" ", i.Info.SpeakableName.ToLowerInvariant().Split(' ').Where(ii => ii != "the")) ==
                       string.Join(" ", name.ToLowerInvariant().Split(' ').Where(ii => ii != "the"))) ??
                   values.FirstOrDefault(i => i.Address.Split('@')[0].ToLowerInvariant() == name.ToLowerInvariant())
                ;
        }

        public static RoomsService.BuildingResult MatchName(this ICollection<RoomsService.BuildingResult> values, string name)
        {
            name = _cleanup.Replace(name, "");
            return values.FirstOrDefault(i => i.Name.ToLowerInvariant() == name.ToLowerInvariant());
        }

        public static RoomsService.RoomStatusResult MatchFloorName(this ICollection<RoomsService.RoomStatusResult> values, string name)
        {
            name = _cleanup.Replace(name, "");
            return values.FirstOrDefault(i => i.Info.FloorName.ToLowerInvariant() == name.ToLowerInvariant());
        }
    }
}