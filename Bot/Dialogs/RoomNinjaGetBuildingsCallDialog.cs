using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class RoomNinjaGetBuildingsCallDialog : RoomNinjaCallDialogBase<RoomsService.BuildingResult[]>
    {
        public RoomNinjaGetBuildingsCallDialog(Uri requestUri) : base(requestUri)
        {
        }

        protected override async Task<RoomsService.BuildingResult[]> DoWork(IDialogContext context, RoomsService api)
        {
            return await api.GetBuildings();
        }
    }
}
