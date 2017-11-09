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
    public class CheckRoomDialog : RoomNinjaDialogBase
    {
        private RoomStatusCriteria _criteria;
        private List<RoomsService.RoomStatusResult> _roomResults;

        public CheckRoomDialog(RoomStatusCriteria criteria, Uri requestUri)
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
            await context.PostAsync(context.CreateMessage($"Checking status of {_criteria}", InputHints.IgnoringInput));

            await context.Forward(new RoomNinjaGetRoomsStatusForBuildingCallDialog(_requestUri, buildingId), GotRoomStatus, context.Activity, new CancellationToken());
        }

        private async Task GotRoomStatus(IDialogContext context, IAwaitable<RoomsService.RoomStatusResult[]> callback)
        {
            var rooms = await callback;
            var room = rooms.MatchName(_criteria.Room);
            // TODO: do we need to use api.GetRoomsStatus(room.Id) ?
            if (null == room)
            {
                await context.PostAsync(context.CreateMessage($"Can't find room {_criteria.Room}", InputHints.AcceptingInput));
            }
            else
            {
                var tz = GetTimezone(context.GetBuildingId());
                var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

                // ok, now we just have rooms that meet the criteria - let's see what's free when asked
                var meetings = room.Status.NearTermMeetings.Where(i => i.End > (_criteria.StartTime ?? now)).OrderBy(i => i.Start).ToList();
                var firstMeeting = meetings.FirstOrDefault();
                var result =
                    (null == firstMeeting)
                        ? new { busy = false, room = room, Until = (DateTime?)null }
                        : (firstMeeting.Start > (_criteria.EndTime ?? _criteria.StartTime ?? now) && !firstMeeting.IsStarted)
                            ? new { busy = false, room = room, Until = (DateTime?)TimeZoneInfo.ConvertTime(firstMeeting.Start, tz) }
                            : new { busy = true, room = room, Until = (DateTime?)TimeZoneInfo.ConvertTime(GetNextFree(meetings), tz) };

                var until = result.Until.HasValue ? $"{result.Until.ToSimpleTime()}" : "";
                var start = _criteria.StartTime.HasValue ? $" at {_criteria.StartTime.ToSimpleTime()}" : "";

                var reason = firstMeeting == null ? "" :
                    !string.IsNullOrEmpty(firstMeeting.Organizer) ?
                        $" by {firstMeeting.Organizer}" :
                        !string.IsNullOrEmpty(firstMeeting.Subject) ?
                            $" for {firstMeeting.Subject}" :
                            "";
                if (result.busy)
                {
                    if (result.Until.HasValue)
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is busy{start}{reason} until {until}", InputHints.AcceptingInput));
                    }
                    else
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is busy{start}{reason}", InputHints.AcceptingInput));
                    }
                }
                else
                {
                    if (result.Until.HasValue)
                    {
                        if (!string.IsNullOrEmpty(reason))
                        {
                            reason = $" when it's reserved{reason}";
                        }
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is free{start} until {until}{reason}", InputHints.AcceptingInput));
                    }
                    else
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is free{start}", InputHints.AcceptingInput));
                    }
                }
            }
            context.Done(string.Empty);
        }

        private DateTime GetNextFree(IEnumerable<RoomsService.RoomStatusResult.MeetingResult> meetings)
        {
            var time = meetings.First().End;
            foreach (var meeting in meetings)
            {
                if (time < meeting.Start)
                    return time;
                time = meeting.End;
            }
            return time;
        }
    }
}
