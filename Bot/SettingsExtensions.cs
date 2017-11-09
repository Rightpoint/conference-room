using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.ConferenceRoom.Bot
{
    public static class SettingsExtensions
    {
        private static readonly string BuildingId = nameof(BuildingId);
        private static readonly string PreferredFloorId = nameof(PreferredFloorId);

        public static string GetBuildingId(this IDialogContext context)
        {
            return context.UserData.TryGetValue(BuildingId, out string value) ? value : null;
        }

        public static void SetBuildingId(this IDialogContext context, string value)
        {
            context.UserData.SetValue(BuildingId, value);
        }

        public static string GetPreferredFloorId(this IDialogContext context)
        {
            return context.UserData.TryGetValue(PreferredFloorId, out string value) ? value : null;
        }

        public static void SetPreferredFloorId(this IDialogContext context, string value)
        {
            context.UserData.SetValue(PreferredFloorId, value);
        }
    }
}
