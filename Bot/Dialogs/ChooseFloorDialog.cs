using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class ChooseFloorDialog : IDialog<string>
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
            var buildingId = context.GetBuildingId();
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"Set your building first", InputHints.AcceptingInput));
                context.Done(string.Empty);
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
            var buildingId = context.GetBuildingId();
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"Set your building first", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            _floorName = (await argument).Text;
            if (null == _floorName)
            {
                context.Done(string.Empty);
                return;
            }

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRooms, context.Activity, new CancellationToken());
        }

        private async Task GotRooms(IDialogContext context, IAwaitable<RoomsService.RoomStatusResult[]> callback)
        {
            var floor = (await callback).MatchFloorName(_floorName);
            if (null == floor)
            {
                await context.PostAsync(context.CreateMessage($"Can't find floor {_floorName}.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else
            {
                context.Done(floor.Id);
            }
        }
    }
}
