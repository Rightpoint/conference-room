using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Criteria;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs.Criteria
{
    [Serializable]
    public class RoomStatusCriteriaDialog : IDialog<RoomStatusCriteria>
    {
        private readonly RoomStatusCriteria _criteria;

        public RoomStatusCriteriaDialog(RoomStatusCriteria criteria)
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
            if (string.IsNullOrEmpty(_criteria.Room))
            {
                await PromptForRoom(context);
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
                context.Done<RoomStatusCriteria>(null);
                return;
            }

            context.Done(_criteria);
        }
    }
}
