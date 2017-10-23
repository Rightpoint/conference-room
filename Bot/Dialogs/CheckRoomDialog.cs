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
    public class CheckRoomDialog : RoomNinjaDialogBase
    {
        private readonly Uri _requestUri;
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

            // searching...
            await context.PostAsync(context.CreateMessage($"Checking status of {_criteria}", InputHints.IgnoringInput));
            
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
            var room = rooms.MatchName(_criteria.Room);
            // TODO: do we need to use api.GetRoomsStatus(room.Id) ?
            if (null == room)
            {
                await context.PostAsync(context.CreateMessage($"Can't find room {_criteria.Room}", InputHints.AcceptingInput));
            }
            else
            {
                var tz = GetTimezone(_criteria.Office ?? RoomBaseCriteria.OfficeOptions.Chicago);
                var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

                // ok, now we just have rooms that meet the criteria - let's see what's free when asked
                var meetings = room.Status.NearTermMeetings.Where(i => i.End >= (_criteria.StartTime ?? now)).OrderBy(i => i.Start).ToList();
                var firstMeeting = meetings.FirstOrDefault();
                var result =
                    (null == firstMeeting)
                        ? new { busy = false, room = room, Until = (DateTime?)null }
                        : (firstMeeting.Start > (_criteria.EndTime ?? now) && !firstMeeting.IsStarted)
                            ? new { busy = false, room = room, Until = (DateTime?)firstMeeting.Start }
                            : new { busy = true, room = room, Until = (DateTime?)null };

                if (result.busy)
                {
                    if (result.Until.HasValue)
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is busy until {result.Until:h:mm tt}", InputHints.AcceptingInput));
                    }
                    else
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is busy", InputHints.AcceptingInput));
                    }
                }
                else
                {
                    if (result.Until.HasValue)
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is free until {result.Until:h:mm tt}", InputHints.AcceptingInput));
                    }
                    else
                    {
                        await context.PostAsync(context.CreateMessage($"{_criteria.Room} is free", InputHints.AcceptingInput));
                    }
                }
            }
            context.Done(string.Empty);
        }
    }
}
