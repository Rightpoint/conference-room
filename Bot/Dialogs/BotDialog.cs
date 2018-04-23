using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Criteria;
using RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria;
using RightpointLabs.ConferenceRoom.Bot.Extensions;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class BotDialog : LuisDialog<object>
    {
        private readonly Uri _requestUri;
        private List<RoomsService.RoomStatusResult> _roomResults;
        private RoomSearchCriteria _criteria;

        public BotDialog(Uri requestUri) : base(new LuisService(new LuisModelAttribute(Config.GetAppSetting("LuisAppId"), Config.GetAppSetting("LuisAPIKey"))))
        {
            _requestUri = requestUri;
        }

        public override Task StartAsync(IDialogContext context)
        {
            return base.StartAsync(context);
        }

        protected override Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {
            return base.MessageReceived(context, item);
        }

        protected override Task DispatchToIntentHandler(IDialogContext context, IAwaitable<IMessageActivity> item, IntentRecommendation bestIntent, LuisResult result)
        {
            Trace.WriteLine($"Intent: {bestIntent.Intent}, Entities: {string.Join(", ", result.Entities.Select(i => i.Type ?? i.Role))}");
            return base.DispatchToIntentHandler(context, item, bestIntent, result);
        }

        [LuisIntent("None")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(context.CreateMessage($"Sorry, I don't know what you meant.  You said: {result.Query}", InputHints.AcceptingInput));
            context.Done(string.Empty);
        }

        [LuisIntent("info")]
        public async Task InfoIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(context.CreateMessage($"This is a bot from Rightpoint Labs Beta to help you find and book conference rooms.  Try 'check garage' or 'find a room at noon'", InputHints.AcceptingInput));
            context.Done(string.Empty);
        }

        [LuisIntent("help")]
        public async Task HelpIntent(IDialogContext context, LuisResult result)
        {
            await context.PostAsync(context.CreateMessage(
                $"This is a Rightpoint Labs Beta app and not supported by the helpdesk.  Submit bugs via https://github.com/RightpointLabs/conference-room/issues or bug Rupp (jrupp@rightpoint.com) if it's broken.",
                $"This is a Rightpoint Labs Beta app and not supported by the helpdesk.  Bug Rupp if it's broken.",
                InputHints.AcceptingInput));
            context.Done(string.Empty);
        }

        [LuisIntent("setSecurity")]
        public async Task SetSecurity(IDialogContext context, LuisResult result)
        {
            var securityLevel = result.Entities.FirstOrDefault(i => i.Type == "securityLevel")?.Entity?.ToLowerInvariant();
            await ProcessSecurityLevelChange(context, securityLevel);
        }

        private static readonly Regex _cleanup = new Regex("[^A-Za-z ]*", RegexOptions.Compiled);
        private async Task ProcessSecurityLevelChange(IDialogContext context, string securityLevel)
        {
            if (!string.IsNullOrEmpty(securityLevel))
            {
                securityLevel = _cleanup.Replace(securityLevel, "");
            }
            if (string.IsNullOrEmpty(securityLevel) || (securityLevel != "high" && securityLevel != "low"))
            {
                await context.PostAsync(context.CreateMessage($"This bot supports two security modes.  Use high when you're always in control.  Use low when used in public (ie. via Cortana Invoke).  What security level would you like - high or low?", InputHints.ExpectingInput));
                context.Wait(SetSecurityValue);
            }
            else
            {
                await context.PostAsync(context.CreateMessage($"Security set to {securityLevel}.", InputHints.AcceptingInput));
                context.SetSecurityLevel(securityLevel);
                context.Done(string.Empty);
            }
        }

        public async Task SetSecurityValue(IDialogContext context, IAwaitable<IMessageActivity> awaitable)
        {
            var text = (await awaitable)?.Text?.ToLowerInvariant();
            if (text == "cancel" || text == "reset")
            {
                context.Done(string.Empty);
                return;
            }
            await ProcessSecurityLevelChange(context, text);
        }

        [LuisIntent("findRoom")]
        public async Task FindRoom(IDialogContext context, LuisResult result)
        {
            var criteria = RoomSearchCriteria.ParseCriteria(result, context.GetTimezone());
            await context.Forward(new RoomSearchCriteriaDialog(criteria), DoRoomSearch, context.Activity, new CancellationToken());
        }

        private async Task DoRoomSearch(IDialogContext context, IAwaitable<RoomSearchCriteria> result)
        {
            var criteria = await result;
            if (null == criteria)
            {
                context.Done(string.Empty);
                return;
            }
            await context.Forward(new FindRoomDialog(criteria, _requestUri), Done, context.Activity, new CancellationToken());
        }

        [LuisIntent("bookRoom")]
        public async Task BookRoom(IDialogContext context, LuisResult result)
        {
            var criteria = RoomBookingCriteria.ParseCriteria(result, context.GetTimezone());
            await context.Forward(new RoomBookingCriteriaDialog(criteria), DoBookRoom, context.Activity, new CancellationToken());
        }

        private async Task DoBookRoom(IDialogContext context, IAwaitable<RoomBookingCriteria> result)
        {
            var criteria = await result;
            if (null == criteria)
            {
                context.Done(string.Empty);
                return;
            }
            await context.Forward(new BookRoomDialog(criteria, _requestUri), Done, context.Activity, new CancellationToken());
        }

        [LuisIntent("checkRoom")]
        public async Task CheckRoom(IDialogContext context, LuisResult result)
        {
            var criteria = RoomStatusCriteria.ParseCriteria(result, context.GetTimezone());
            await context.Forward(new RoomStatusCriteriaDialog(criteria), DoRoomCheck, context.Activity, new CancellationToken());
        }

        private async Task DoRoomCheck(IDialogContext context, IAwaitable<RoomStatusCriteria> result)
        {
            var criteria = await result;
            if (null == criteria)
            {
                context.Done(string.Empty);
                return;
            }
            await context.Forward(new CheckRoomDialog(criteria, _requestUri), Done, context.Activity, new CancellationToken());
        }

        [LuisIntent("setBuilding")]
        public async Task SetBuilding(IDialogContext context, LuisResult result)
        {
            var buildingName = result.Entities.Where(i => i.Type == "building").Select(i => i.Entity).FirstOrDefault();
            await context.Forward(new ChooseBuildingDialog(_requestUri, buildingName), SetBuildingCallback, context.Activity, new CancellationToken());
        }

        private async Task SetBuildingCallback(IDialogContext context, IAwaitable<BuildingChoice> result)
        {
            var building = await result;
            if (null != building)
            {
                context.SetBuilding(building);
                await context.PostAsync(context.CreateMessage($"Building set to {building.BuildingName}.", InputHints.AcceptingInput));
            }
            context.Done(string.Empty);
        }

        [LuisIntent("setFloor")]
        public async Task SetFloor(IDialogContext context, LuisResult result)
        {
            var buildingName = result.Entities.Where(i => i.Type == "floor").Select(i => i.Entity).FirstOrDefault();
            await context.Forward(new ChooseFloorDialog(_requestUri, buildingName), SetFloorCallback, context.Activity, new CancellationToken());
        }

        private async Task SetFloorCallback(IDialogContext context, IAwaitable<FloorChoice> result)
        {
            var floor = await result;
            if (null != floor)
            {
                context.SetPreferredFloor(floor);
                await context.PostAsync(context.CreateMessage($"Preferred floor set to {floor.FloorName}.", InputHints.AcceptingInput));
            }
            context.Done(string.Empty);
        }

        [LuisIntent("clearFloor")]
        public async Task ClearFloor(IDialogContext context, LuisResult result)
        {
            context.SetPreferredFloor(null);
            await context.PostAsync(context.CreateMessage($"Preferred floor cleared.", InputHints.AcceptingInput));
            context.Done(string.Empty);
        }

        private async Task Done(IDialogContext context, IAwaitable<string> result)
        {
            await result;
            context.Done(string.Empty);
        }
    }
}

