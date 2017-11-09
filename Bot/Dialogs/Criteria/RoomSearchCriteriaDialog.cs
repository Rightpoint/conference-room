using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Criteria;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria
{
    [Serializable]
    public class RoomSearchCriteriaDialog : IDialog<RoomSearchCriteria>
    {
        private readonly RoomSearchCriteria _criteria;

        public RoomSearchCriteriaDialog(RoomSearchCriteria criteria)
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
            if (!_criteria.NumberOfPeople.HasValue)
            {
                await PromptForSize(context);
                return;
            }
            if (null == _criteria.Equipment || !_criteria.Equipment.Any())
            {
                await PromptForEquipment(context);
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

        private async Task PromptForSize(IDialogContext context)
        {
            await context.PostAsync(context.CreateMessage($"For how many people?", InputHints.ExpectingInput));
            context.Wait(GetSize);
        }

        private static readonly Regex _sizeCleanup = new Regex("[^0-9]*", RegexOptions.Compiled);

        public async Task GetSize(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var text = _sizeCleanup.Replace((await argument).Text, "");
            if (string.IsNullOrEmpty(text) || text.ToLowerInvariant() == "cancel" || text.ToLowerInvariant() == "stop")
            {
                await context.PostAsync(context.CreateMessage($"Cancelling.", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            _criteria.NumberOfPeople = int.Parse(text);
            await PromptNext(context);
        }

        private async Task PromptForEquipment(IDialogContext context)
        {
            await context.PostAsync(context.CreateMessage($"What equipment do you need?", InputHints.ExpectingInput));
            context.Wait(GetEquipment);
        }

        public async Task GetEquipment(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var text = (await argument).Text;
            if (string.IsNullOrEmpty(text) || text.ToLowerInvariant() == "cancel" || text.ToLowerInvariant() == "stop")
            {
                await context.PostAsync(context.CreateMessage($"Cancelling.", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            _criteria.ParseEquipment(text);

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
                context.Done(string.Empty);
                return;
            }

            var svc = GetLuisService();
            var result = await svc.QueryAsync(text, CancellationToken.None);
            _criteria.LoadTimeCriteria(result);

            if (!_criteria.StartTime.HasValue)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I couldn't understand that start time.", InputHints.AcceptingInput));
                context.Done(string.Empty);
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
                context.Done(string.Empty);
                return;
            }

            var svc = GetLuisService();
            var result = await svc.QueryAsync(text, CancellationToken.None);
            _criteria.LoadEndTimeCriteria(result);

            if (!_criteria.EndTime.HasValue)
            {
                await context.PostAsync(context.CreateMessage($"Sorry, I couldn't understand that end time or duration.", InputHints.AcceptingInput));
                context.Done(string.Empty);
                return;
            }

            await PromptNext(context);
        }

        private ILuisService GetLuisService()
        {
            return new LuisService(new LuisModelAttribute(Utils.GetAppSetting("LuisAppId"), Utils.GetAppSetting("LuisAPIKey")));
        }
    }
}
