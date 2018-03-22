using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class ChooseFloorDialog : IDialog<FloorChoice>
    {
        private Uri _requestUri;
        private string _floorName;

        public ChooseFloorDialog(Uri requestUri, string floorName)
        {
            _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
            _floorName = floorName;
        }

        public async Task StartAsync(IDialogContext context)
        {
            // without this wait, we'd double-process the last message recieved
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await context.Forward(new GetBuildingDialog(_requestUri), GotBuilding, context.Activity, new CancellationToken());
        }

        public async Task GotBuilding(IDialogContext context, IAwaitable<BuildingChoice> argument)
        {
            var building = await argument;
            var buildingId = building?.BuildingId;
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"Set your building first with the 'set building' command", InputHints.AcceptingInput));
                context.Done<FloorChoice>(null);
                return;
            }

            if (null == _floorName)
            {
                await context.PostAsync(context.CreateMessage($"What is your default floor?", InputHints.ExpectingInput));
                context.Wait(GetFloorName);
                return;
            }

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRooms, context.Activity, new CancellationToken());
        }

        public async Task GetFloorName(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var buildingId = context.GetBuilding()?.BuildingId;
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"Set your building first with the 'set building' command", InputHints.AcceptingInput));
                context.Done<FloorChoice>(null);
                return;
            }

            _floorName = (await argument).Text;
            if (null == _floorName)
            {
                context.Done<FloorChoice>(null);
                return;
            }

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRooms, context.Activity, new CancellationToken());
        }

        private async Task GotRooms(IDialogContext context, IAwaitable<RoomsService.RoomStatusResult[]> callback)
        {
            var roomOnFloor = (await callback).MatchFloorName(_floorName);
            if (null == roomOnFloor)
            {
                await context.PostAsync(context.CreateMessage($"Can't find floor {_floorName}.", InputHints.AcceptingInput));
                context.Done<FloorChoice>(null);
            }
            else
            {
                context.Done(new FloorChoice()
                {
                    Floor = roomOnFloor.Info.Floor,
                    FloorId = roomOnFloor.Info.FloorId,
                    FloorName = roomOnFloor.Info.FloorName,
                });
            }
        }
    }
}
