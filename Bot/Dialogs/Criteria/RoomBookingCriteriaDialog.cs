using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Criteria;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria
{
    [Serializable]
    public class RoomBookingCriteriaDialog : IDialog<RoomBookingCriteria>
    {
        private readonly RoomBookingCriteria _criteria;

        public RoomBookingCriteriaDialog(RoomBookingCriteria criteria)
        {
            _criteria = criteria;
        }

        public async Task StartAsync(IDialogContext context)
        {
            // without this wait, we'd double-process the last message recieved
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await PromptNext(context);
        }

        private async Task PromptNext(IDialogContext context)
        {
            if (string.IsNullOrEmpty(_criteria.Room))
            {
                await PromptForRoom(context);
                return;
            }
            if (!_criteria.StartTime.HasValue)
            {
                await PromptForStartTime(context);
                return;
            }
            if (!_criteria.EndTime.HasValue)
            {
                await PromptForEndTime(context);
                return;
            }

            context.Done(_criteria);
        }

        private async Task PromptForRoom(IDialogContext context)
        {
            await context.PostAsync(context.CreateMessage($"For what room?", InputHints.ExpectingInput));
            context.Wait(GetRoom);
        }

        public async Task GetRoom(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            _criteria.Room = (await argument).Text;
            if (string.IsNullOrEmpty(_criteria.Room))
            {
                context.Done<RoomBookingCriteria>(null);
                return;
            }

            await PromptNext(context);
        }

        private async Task PromptForStartTime(IDialogContext context)
        {
            await context.PostAsync(context.CreateMessage($"Starting when?", InputHints.ExpectingInput));
            context.Wait(GetStartTime);
        }

        public async Task GetStartTime(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var text = (await argument).Text;
            if (string.IsNullOrEmpty(text) || text.ToLowerInvariant() == "cancel" || text.ToLowerInvariant() == "stop")
            {
                context.Done<RoomBookingCriteria>(null);
                return;
            }

            var svc = GetLuisService();
            var result = await svc.QueryAsync(text, CancellationToken.None);
            _criteria.LoadTimeCriteria(result, context.GetTimezone());

            if (!_criteria.StartTime.HasValue)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I couldn't understand that start time.", InputHints.IgnoringInput));
                await PromptForStartTime(context);
                return;
            }

            await PromptNext(context);
        }

        private async Task PromptForEndTime(IDialogContext context)
        {
            await context.PostAsync(context.CreateMessage($"Ending when?", InputHints.ExpectingInput));
            context.Wait(GetEndTime);
        }

        public async Task GetEndTime(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var text = (await argument).Text;
            if (string.IsNullOrEmpty(text) || text.ToLowerInvariant() == "cancel" || text.ToLowerInvariant() == "stop")
            {
                context.Done<RoomBookingCriteria>(null);
                return;
            }

            var svc = GetLuisService();
            var result = await svc.QueryAsync(text, CancellationToken.None);
            _criteria.LoadEndTimeCriteria(result, context.GetTimezone());

            if (!_criteria.EndTime.HasValue)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I couldn't understand that end time or duration.", InputHints.IgnoringInput));
                await PromptForEndTime(context);
                return;
            }

            await PromptNext(context);
        }

        private ILuisService GetLuisService()
        {
            return new LuisService(new LuisModelAttribute(Config.GetAppSetting("LuisAppId"), Config.GetAppSetting("LuisAPIKey")));
        }
    }
}
