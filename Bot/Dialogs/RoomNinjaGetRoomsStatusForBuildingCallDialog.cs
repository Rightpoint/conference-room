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
    public class RoomNinjaGetRoomsStatusForBuildingCallDialog : RoomNinjaCallDialogBase<RoomsService.RoomStatusResult[]>
    {
        private readonly string _buildingId;

        public RoomNinjaGetRoomsStatusForBuildingCallDialog(Uri requestUri, string buildingId) : base(requestUri)
        {
            _buildingId = buildingId;
        }

        protected override async Task<RoomsService.RoomStatusResult[]> DoWork(IDialogContext context, RoomsService api)
        {
            return await api.GetRoomsStatusForBuilding(_buildingId);
        }
    }
}
