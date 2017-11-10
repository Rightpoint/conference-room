using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
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

            var buildingId = context.GetBuildingId();
            if (string.IsNullOrEmpty(buildingId))
            {
                await context.PostAsync(context.CreateMessage($"You need to set a building first", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            // searching...
            await context.PostAsync(context.CreateMessage($"Searching for {_criteria}", InputHints.IgnoringInput));

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRoomStatus, context.Activity, new CancellationToken());
        }

        private async Task GotRoomStatus(IDialogContext context, IAwaitable<RoomsService.RoomStatusResult[]> callback)
        {
            var rooms = await callback;
            var tz = GetTimezone(context.GetBuildingId());
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

            var preferredFloorId = context.GetPreferredFloorId();

            // ok, now we just have rooms that meet the criteria - let's see what's free when asked
            _roomResults = rooms.Select(r =>
            {
                var meetings = r.Status.NearTermMeetings.Where(i => i.End > _criteria.StartTime).OrderBy(i => i.Start).ToList();
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
                .OrderBy(i => i.Info.FloorId == preferredFloorId ? 0 : 1)
                .ThenBy(i => i.Info.Size)
                .ThenBy(i => i.Info.Equipment?.Count)
                .ToList();

            if (_roomResults.Count == 0)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, no free rooms meet that criteria", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else if (_roomResults.Count == 1)
            {
                await context.PostAsync(context.CreateMessage($"{_roomResults[0].Info.SpeakableName} is free - would you like to reserve it?", InputHints.ExpectingInput));
                context.Wait(ConfirmBookOneRoom);
            }
            else
            {
                await PromptForMultipleRooms(context);
            }
        }

        private async Task PromptForMultipleRooms(IDialogContext context)
        {
            var msg = $"{_roomResults.Count} rooms are available, which do you want?  {string.Join(", ", _roomResults.Select(i => i.Info.SpeakableName).Take(5))}.";
            await context.PostAsync(context.CreateMessage(msg, InputHints.ExpectingInput));
            context.Wait(ConfirmBookOneOfManyRooms);
        }

        private static readonly Regex _cleanup = new Regex("[^A-Za-z ]*", RegexOptions.Compiled);

        private async Task ConfirmBookOneRoom(IDialogContext context, IAwaitable<IMessageActivity> awaitable)
        {
            var result = await awaitable;
            var answer = _cleanup.Replace(result.Text ?? "", "");
            if (answer.ToLowerInvariant() == "yes" || answer.ToLowerInvariant() == "ok")
            {
                await BookIt(context, _roomResults[0].Id, _criteria.StartTime, _criteria.EndTime);
            }
            else
            {
                await context.PostAsync(context.CreateMessage("Not booking room.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
        }

        private async Task ConfirmBookOneOfManyRooms(IDialogContext context, IAwaitable<IMessageActivity> awaitable)
        {
            var result = await awaitable;
            var answer = _cleanup.Replace(result.Text ?? "", "");
            var room = _roomResults.MatchName(answer);
            if (answer.ToLowerInvariant() == "no" || answer.ToLowerInvariant() == "cancel")
            {
                await context.PostAsync(context.CreateMessage("OK.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else if (room != null)
            {
                await BookIt(context, room.Id, _criteria.StartTime, _criteria.EndTime);
            }
            else
            {
                await PromptForMultipleRooms(context);
            }
        }
    }
}
