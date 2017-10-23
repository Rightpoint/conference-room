using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
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

            var building = (await api.GetBuildings()).FirstOrDefault(i => i.Name == criteria.Office.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {criteria.Office}", InputHints.AcceptingInput));
            }
            else
            {
                var rooms = await api.GetRoomsStatusForBuilding(building.Id);
                var tz = GetTimezone(criteria.Office ?? RoomBaseCriteria.OfficeOptions.Chicago);
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
                await BookIt(context, _roomResults[0].Id, _criteria.StartTime, _criteria.EndTime);
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
                await BookIt(context, room.Id, _criteria.StartTime, _criteria.EndTime);
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
            var criteria = RoomBookingCriteria.ParseCriteria(result);
            var dialog = new FormDialog<RoomBookingCriteria>(criteria, RoomBookingCriteria.BuildForm, entities: result.Entities);
            await context.Forward(dialog, DoBookRoom, context.Activity, new CancellationToken());
        }

        private async Task DoBookRoom(IDialogContext context, IAwaitable<RoomBookingCriteria> callback)
        {
            var criteria = await callback;
            if (null == criteria)
            {
                context.Wait(MessageReceived);
                return;
            }

            // searching...
            await context.PostAsync(context.CreateMessage($"Booking {criteria}", InputHints.IgnoringInput));

            var api = new RoomsService(Utils.GetAppSetting("RoomNinjaApiAccessToken"));

            var building = (await api.GetBuildings()).FirstOrDefault(i => i.Name == criteria.Office.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {criteria.Office}", InputHints.AcceptingInput));
            }
            else
            {
                var rooms = await api.GetRoomsForBuilding(building.Id);
                var room = rooms.FirstOrDefault(i => i.Info.SpeakableName == criteria.Room);
                if (null == room)
                {
                    await context.PostAsync(context.CreateMessage($"Can't find room {criteria.Room}", InputHints.AcceptingInput));
                }
                else
                {
                    await BookIt(context, room.Id, criteria.StartTime, criteria.EndTime);
                }
            }

            context.Wait(MessageReceived);
        }


        [LuisIntent("checkRoom")]
        public async Task CheckRoom(IDialogContext context, LuisResult result)
        {
            var criteria = RoomStatusCriteria.ParseCriteria(result);
            var dialog = new FormDialog<RoomStatusCriteria>(criteria, RoomStatusCriteria.BuildForm, entities: result.Entities);
            await context.Forward(dialog, DoRoomCheck, context.Activity, new CancellationToken());
        }

        private async Task DoRoomCheck(IDialogContext context, IAwaitable<RoomStatusCriteria> callback)
        {
            var criteria = await callback;
            if (null == criteria)
            {
                context.Wait(MessageReceived);
                return;
            }

            // searching...
            await context.PostAsync(context.CreateMessage($"Checking status of {criteria}", InputHints.IgnoringInput));

            var api = new RoomsService(Utils.GetAppSetting("RoomNinjaApiAccessToken"));

            var building = (await api.GetBuildings()).FirstOrDefault(i => i.Name == criteria.Office.ToString());
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {criteria.Office}", InputHints.AcceptingInput));
            }
            else
            {
                var rooms = await api.GetRoomsForBuilding(building.Id);
                var room = rooms.FirstOrDefault(i => i.Info.SpeakableName == criteria.Room);
                if (null == room)
                {
                    await context.PostAsync(context.CreateMessage($"Can't find room {criteria.Room}", InputHints.AcceptingInput));
                }
                else
                {
                    var r = await api.GetRoomsStatus(room.Id);
                    var tz = GetTimezone(criteria.Office ?? RoomBaseCriteria.OfficeOptions.Chicago);
                    var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);

                    // ok, now we just have rooms that meet the criteria - let's see what's free when asked
                    var meetings = r.Status.NearTermMeetings.Where(i => i.End >= (criteria.StartTime ?? now)).OrderBy(i => i.Start).ToList();
                    var firstMeeting = meetings.FirstOrDefault();
                    var result =
                        (null == firstMeeting)
                            ? new {busy = false, room = r, Until = (DateTime?) null}
                            : (firstMeeting.Start > (criteria.EndTime ?? now) && !firstMeeting.IsStarted)
                                ? new {busy = false, room = r, Until = (DateTime?)firstMeeting.Start }
                                : new {busy = true, room = r, Until = (DateTime?) null };

                    if (result.busy)
                    {
                        if (result.Until.HasValue)
                        {
                            await context.PostAsync(context.CreateMessage($"{criteria.Room} is busy until {result.Until:h:mm tt}", InputHints.AcceptingInput));
                        }
                        else
                        {
                            await context.PostAsync(context.CreateMessage($"{criteria.Room} is busy", InputHints.AcceptingInput));
                        }
                    }
                    else
                    {
                        if (result.Until.HasValue)
                        {
                            await context.PostAsync(context.CreateMessage($"{criteria.Room} is free until {result.Until:h:mm tt}", InputHints.AcceptingInput));
                        }
                        else
                        {
                            await context.PostAsync(context.CreateMessage($"{criteria.Room} is free", InputHints.AcceptingInput));
                        }
                    }
                }
            }

            context.Wait(MessageReceived);
        }

        private async Task BookIt(IDialogContext context, string roomId, DateTime? criteriaStartTime, DateTime? criteriaEndTime)
        {
            await context.PostAsync(context.CreateMessage("Booking not implemented yet.", InputHints.AcceptingInput));
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

