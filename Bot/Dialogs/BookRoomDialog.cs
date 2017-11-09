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

            // searching...
            await context.PostAsync(context.CreateMessage($"Booking {_criteria}", InputHints.IgnoringInput));

            await context.Forward(new RoomNinjaGetBuildingsCallDialog(_requestUri), GotBuildings, context.Activity, new CancellationToken());
        }

        private async Task GotBuildings(IDialogContext context, IAwaitable<RoomsService.BuildingResult[]> callback)
        {
            var building = (await callback).FirstOrDefault(i => i.Name == _criteria.BuildingId.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {_criteria.BuildingId}", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else
            {
                await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, building.Id), GotRoomStatus, context.Activity, new CancellationToken());
            }
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
