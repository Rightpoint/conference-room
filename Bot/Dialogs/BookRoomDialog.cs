using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Criteria;
using RightpointLabs.ConferenceRoom.Bot.Services;
using RightpointLabs.ConferenceRoom.Bot.Models;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class BookRoomDialog : RoomNinjaDialogBase
    {
        private RoomBookingCriteria _criteria;
        private List<RoomsService.RoomStatusResult> _roomResults;

        public BookRoomDialog(RoomBookingCriteria criteria, Uri requestUri)
        {
            _criteria = criteria;
            _requestUri = requestUri;
        }

        public override async Task StartAsync(IDialogContext context)
        {
            // without this wait, we'd double-process the last message recieved
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            if (null == _criteria)
            {
                context.Done(string.Empty);
                return;
            }

            await context.Forward(new GetBuildingDialog(_requestUri), GotBuilding, context.Activity, new CancellationToken());
        }

        public async Task GotBuilding(IDialogContext context, IAwaitable<BuildingChoice> argument)
        {
            var building = await argument;
            var buildingId = building?.BuildingId;
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"Set your building first with the 'set building' command", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            // searching...
            await context.PostAsync(context.CreateMessage($"Booking {_criteria}", InputHints.IgnoringInput));

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRoomStatus, context.Activity, new CancellationToken());
        }

        private async Task GotRoomStatus(IDialogContext context, IAwaitable<RoomsService.RoomStatusResult[]> callback)
        {
            var rooms = await callback;
            var room = rooms.MatchName(_criteria.Room);
            if (null == room)
            {
                var speak = $"Can't find room {_criteria.Room}";
                await context.PostAsync(context.CreateMessage($"{speak}, options: {string.Join(", ", rooms.Select(i => i.Info.SpeakableName))}", speak, InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else
            {
                await BookIt(context, room.Id, _criteria.StartTime, _criteria.EndTime);
            }
        }
    }
}
