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
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class FindRoomDialog : RoomNinjaDialogBase
    {
        private RoomSearchCriteria _criteria;
        private List<RoomsService.RoomStatusResult> _roomResults;

        public FindRoomDialog(RoomSearchCriteria criteria, Uri requestUri)
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
            await context.PostAsync(context.CreateMessage($"Searching for {_criteria}", InputHints.IgnoringInput));

            await context.Forward(new RoomNinjaGetBuildingsCallDialog(_requestUri), GotBuildings, context.Activity, new CancellationToken());
        }

        private async Task GotBuildings(IDialogContext context, IAwaitable<RoomsService.BuildingResult[]> callback)
        {
            var building = (await callback).FirstOrDefault(i => i.Name == _criteria.Office.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {_criteria.Office}", InputHints.AcceptingInput));
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
            var tz = GetTimezone(_criteria.Office ?? RoomBaseCriteria.OfficeOptions.Chicago);
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

            if (_criteria.NumberOfPeople.HasValue)
            {
                if (rooms.Any(r => r.Info.Size > 0))
                {
                    rooms = rooms.Where(r => r.Info.Size >= _criteria.NumberOfPeople).ToArray();
                }
                else
                {
                    await context.PostAsync(context.CreateMessage($"Not checking room size - data not available for that building", InputHints.IgnoringInput));
                }
            }
            var equiValues = (_criteria.Equipment ?? new List<RoomSearchCriteria.EquipmentOptions>()).Where(i => i != RoomSearchCriteria.EquipmentOptions.None).ToArray();
            if (equiValues.Any())
            {
                if (rooms.Any(r => (r.Info.Equipment ?? new List<RoomSearchCriteria.EquipmentOptions>()).Any()))
                {
                    rooms = rooms.Where(r => equiValues.All(e => (r.Info.Equipment ?? new List<RoomSearchCriteria.EquipmentOptions>()).Contains(e))).ToArray();
                }
                else
                {
                    await context.PostAsync(context.CreateMessage($"Not checking room equipment - data not available for that building", InputHints.IgnoringInput));
                }
            }

            // ok, now we just have rooms that meet the criteria - let's see what's free when asked
            _roomResults = rooms.Select(r =>
            {
                var meetings = r.Status.NearTermMeetings.Where(i => i.End >= _criteria.StartTime).OrderBy(i => i.Start).ToList();
                var firstMeeting = meetings.FirstOrDefault();
                if (null == firstMeeting)
                {
                    return new { busy = false, room = r };
                }
                else if (firstMeeting.Start > _criteria.EndTime && !firstMeeting.IsStarted)
                {
                    return new { busy = false, room = r };
                }
                else
                {
                    return new { busy = true, room = r };
                }
            })
                .Where(i => !i.busy)
                .Select(i => i.room)
                .OrderBy(i => i.Info.Size)
                .ThenBy(i => i.Info.Equipment?.Count)
                .ToList();

            if (_roomResults.Count == 0)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, no free rooms meet that criteria", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else if (_roomResults.Count == 1)
            {
                PromptDialog.Confirm(context, ConfirmBookOneRoom, $"{_roomResults[0].Info.SpeakableName} is free - would you like to reserve it?");
            }
            else
            {
                PromptDialog.Choice(context,
                    ConfirmBookOneOfManyRooms,
                    _roomResults.Select(i => i.Info.SpeakableName),
                    $"{_roomResults.Count} rooms are available, which do you want?");
            }
        }

        private async Task ConfirmBookOneRoom(IDialogContext context, IAwaitable<bool> awaitable)
        {
            var result = await awaitable;
            if (result)
            {
                await BookIt(context, _roomResults[0].Id, _criteria.StartTime, _criteria.EndTime);
            }
            else
            {
                await context.PostAsync(context.CreateMessage("OK.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
        }

        private async Task ConfirmBookOneOfManyRooms(IDialogContext context, IAwaitable<string> awaitable)
        {
            var answer = await awaitable;
            var room = _roomResults.MatchName(answer);
            if (room != null)
            {
                await BookIt(context, room.Id, _criteria.StartTime, _criteria.EndTime);
            }
            else if (answer == "no" || answer == "cancel")
            {
                await context.PostAsync(context.CreateMessage("OK.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I didn't understand - please answer {string.Join(", ", _roomResults.Take(5).Select(i => i.Info.DisplayName))} or cancel.", InputHints.ExpectingInput));
                PromptDialog.Choice(context,
                    ConfirmBookOneOfManyRooms,
                    _roomResults.Select(i => i.Info.SpeakableName),
                    $"{_roomResults.Count} rooms are available, which do you want?");
            }
        }
    }
}
