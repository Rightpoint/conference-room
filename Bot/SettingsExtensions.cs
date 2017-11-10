using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using RightpointLabs.ConferenceRoom.Bot.Models;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class SettingsExtensions
    {
        private static readonly string Building = nameof(Building);
        private static readonly string PreferredFloor = nameof(PreferredFloor);

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
    }
}
