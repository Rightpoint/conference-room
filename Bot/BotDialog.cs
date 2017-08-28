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

namespace RightpointLabs.ConferenceRoom.Bot
{
    [Serializable]
    public class BotDialog : LuisDialog<object>
    {
        public BotDialog() : base(new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey"))))
        {
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
            var searchMsg = $"Searching for {criteria}";
            var msg = context.MakeMessage();
            msg.Text = searchMsg;
            msg.Speak = searchMsg;
            msg.InputHint = InputHints.IgnoringInput;
            await context.PostAsync(msg);

            await Task.Delay(TimeSpan.FromSeconds(1));

            var msg2 = context.MakeMessage();
            msg2.Text = "Sorry, search not implemented yet";
            msg2.Speak = "Sorry, search not implemented yet";
            msg2.InputHint = InputHints.AcceptingInput;
            await context.PostAsync(msg2);

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

