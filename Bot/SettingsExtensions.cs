using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class SettingsExtensions
    {
        private static readonly string Building = nameof(Building);
        private static readonly string PreferredFloor = nameof(PreferredFloor);
        private static readonly string SecurityLevel = nameof(SecurityLevel);

        public static BuildingChoice GetBuilding(this IDialogContext context)
        {
            return context.UserData.TryGetValue(Building, out string value) ? JObject.Parse(value).ToObject<BuildingChoice>() : null;
        }

        public static void SetBuilding(this IDialogContext context, BuildingChoice value)
        {
            context.UserData.SetValue(Building, null == value ? null : JObject.FromObject(value).ToString());
        }

        public static FloorChoice GetPreferredFloor(this IDialogContext context)
        {
            return context.UserData.TryGetValue(PreferredFloor, out string value) ? JObject.Parse(value).ToObject<FloorChoice>() : null;
        }

        public static void SetPreferredFloor(this IDialogContext context, FloorChoice value)
        {
            context.UserData.SetValue(PreferredFloor, null == value ? null : JObject.FromObject(value).ToString());
        }

        public static SecurityLevel GetSecurityLevel(this IDialogContext context)
        {
            return context.UserData.TryGetValue(SecurityLevel, out int value) ? (SecurityLevel) value : Models.SecurityLevel.High;
        }

        public static void SetSecurityLevel(this IDialogContext context, string value)
        {
            var level = value == "low" ? Models.SecurityLevel.Low : Models.SecurityLevel.High;
            if (level == Models.SecurityLevel.Low)
            {
                // clear out cached settings that could be used for high-security
                var toClear = new[]
                {
                    new RoomNinjaCustomTokenDialog(context, null, null, false, false).CacheKey,
                    new RoomNinjaCustomTokenDialog.CustomResourceAuthTokenDialog(null, RoomsService.Resource, false, false).CacheKey,
                    new RoomNinjaCustomTokenDialog.CustomAppAuthTokenDialog(null, false, false).CacheKey,
                    nameof(LoginState.LastUpn),
                };
                foreach (var key in toClear)
                {
                    context.UserData.RemoveValue(key);
                }
            }
            context.UserData.SetValue(SecurityLevel, (int)level);
        }

        public static TimeZoneInfo GetTimezone(this IDialogContext context)
        {
            var tzId = context.GetBuilding()?.TimezoneId;
            return string.IsNullOrEmpty(tzId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
    }
}
