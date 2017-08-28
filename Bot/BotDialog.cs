using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chronic;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class BotDialog : LuisDialog<object>
    {
        private List<RoomsService.RoomStatusResult> _roomResults;
        private RoomSearchCriteria _criteria;

        public BotDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
        {
        }

        public override Task StartAsync(IDialogContext context)
        {
            return base.StartAsync(context);
        }

        protected override Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            return base.MessageReceived(context, item);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Sorry, I don't know what you meant.  You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        [LuisIntent("findRoom")]
        public async Task FindRoom(IDialogContext context, LuisResult result)
        {
            var criteria = RoomSearchCriteria.ParseCriteria(result);
            var dialog = new FormDialog<RoomSearchCriteria>(criteria, RoomSearchCriteria.BuildForm, entities: result.Entities);
            await context.Forward(dialog, DoRoomSearch, context.Activity, new CancellationToken());
        }

        private async Task DoRoomSearch(IDialogContext context, IAwaitable<RoomSearchCriteria> callback)
        {
            var criteria = await callback;
            if (null == criteria)
            {
                context.Wait(MessageReceived);
                return;
            }

            // searching...
            await context.PostAsync(context.CreateMessage($"Searching for {criteria}", InputHints.IgnoringInput));

            var api = new RoomsService(Utils.GetAppSetting("RoomNinjaApiAccessToken"));

            var building = (await api.GetBuildings()).FirstOrDefault(i => i.Name == criteria.office.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {criteria.office}", InputHints.AcceptingInput));
            }
            else
            {
                var rooms = await api.GetRoomsStatusForBuilding(building.Id);
                var tz = GetTimezone(criteria.office ?? RoomBaseCriteria.OfficeOptions.Chicago);
                var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

                if (criteria.NumberOfPeople.HasValue)
                {
                    if (rooms.Any(r => r.Info.Size > 0))
                    {
                        rooms = rooms.Where(r => r.Info.Size >= criteria.NumberOfPeople).ToArray();
                    }
                    else
                    {
                        await context.PostAsync(context.CreateMessage($"Not checking room size - data not available for that building", InputHints.IgnoringInput));
                    }
                }
                var equiValues = (criteria.Equipment ?? new List<RoomSearchCriteria.EquipmentOptions>()).Where(i => i != RoomSearchCriteria.EquipmentOptions.None).ToArray();
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
                    var meetings = r.Status.NearTermMeetings.Where(i => i.End >= criteria.StartTime).OrderBy(i => i.Start).ToList();
                    var firstMeeting = meetings.FirstOrDefault();
                    if (null == firstMeeting)
                    {
                        return new { busy = false, room = r };
                    }
                    else if (firstMeeting.Start > criteria.EndTime && !firstMeeting.IsStarted)
                    {
                        return new { busy = false, room = r };
                    }
                    else
                    {
                        return new { busy = true, room = r };
                    }
                })
                    .Where(i=> !i.busy)
                    .Select(i => i.room)
                    .OrderBy(i => i.Info.Size)
                    .ThenBy(i => i.Info.Equipment?.Count)
                    .ToList();

                if (_roomResults.Count == 0)
                {
                    await context.PostAsync(context.CreateMessage($"Sorry, no free rooms meet that criteria", InputHints.AcceptingInput));
                }
                else if (_roomResults.Count == 1)
                {
                    _criteria = criteria;

                    PromptDialog.Confirm(context, ConfirmBookOneRoom, $"{_roomResults[0].Info.SpeakableName} is free - would you like to reserve it?");
                    return;
                }
                else
                {
                    _criteria = criteria;

                    PromptDialog.Choice(context, 
                        ConfirmBookOneOfManyRooms,
                        _roomResults.Select(i => i.Info.SpeakableName),
                        $"{_roomResults.Count} rooms are available, which do you want?");
                    return;
                }
            }

            context.Wait(MessageReceived);
        }

        private async Task ConfirmBookOneRoom(IDialogContext context, IAwaitable<bool> awaitable)
        {
            var result = await awaitable;
            if (result)
            {
                //new RoomReservationCriteria(_criteria) {Room = _roomResults[0].Info.DisplayName};
                await context.PostAsync(context.CreateMessage("Booking not implemented yet.", InputHints.AcceptingInput));
            }
            else
            {
                await context.PostAsync(context.CreateMessage("OK.", InputHints.AcceptingInput));
            }

            context.Wait(MessageReceived);
        }

        private async Task ConfirmBookOneOfManyRooms(IDialogContext context, IAwaitable<string> awaitable)
        {
            var answer = await awaitable;
            var room = _roomResults.FirstOrDefault(i => i.Info.SpeakableName == answer);
            if (room != null)
            {
                //new RoomReservationCriteria(_criteria) {Room = room.Info.DisplayName};
                await context.PostAsync(context.CreateMessage("Booking not implemented yet.", InputHints.AcceptingInput));
            }
            else if (answer == "no" || answer == "cancel")
            {
                await context.PostAsync(context.CreateMessage("OK.", InputHints.AcceptingInput));
            }
            else
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I didn't understand - please answer {string.Join(", ", _roomResults.Take(5).Select(i => i.Info.DisplayName))} or cancel.", InputHints.ExpectingInput));
            }

            context.Wait(MessageReceived);
        }

        [LuisIntent("bookRoom")]
        public async Task BookRoom(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"You have reached the bookRoom intent. You said: {result.Query}"); //
            context.Wait(MessageReceived);
        }

        private TimeZoneInfo GetTimezone(RoomSearchCriteria.OfficeOptions office)
        {
            switch (office)
            {
                case RoomSearchCriteria.OfficeOptions.Atlanta:
                case RoomSearchCriteria.OfficeOptions.Boston:
                case RoomSearchCriteria.OfficeOptions.Detroit:
                    return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                case RoomSearchCriteria.OfficeOptions.Chicago:
                case RoomSearchCriteria.OfficeOptions.Dallas:
                    return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                case RoomSearchCriteria.OfficeOptions.Denver:
                    return TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
                case RoomSearchCriteria.OfficeOptions.Los_Angeles:
                    return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            return null;
        }
    }
}

