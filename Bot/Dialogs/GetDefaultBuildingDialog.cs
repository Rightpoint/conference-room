using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Extensions;
using RightpointLabs.ConferenceRoom.Bot.Models;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class GetDefaultBuildingDialog : IDialog<BuildingChoice>
    {
        private Uri _requestUri;

        public GetDefaultBuildingDialog(Uri requestUri)
        {
            _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        }

        public async Task StartAsync(IDialogContext context)
        {
            // without this wait, we'd double-process the last message recieved
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var building = context.GetBuilding();
            if (null != building)
            {
                context.Done(building);
                return;
            }

            await context.Forward(new ChooseBuildingDialog(_requestUri, null), GotBuilding, context.Activity, new CancellationToken());
        }

        public async Task GotBuilding(IDialogContext context, IAwaitable<BuildingChoice> argument)
        {
            var building = await argument;
            context.SetBuilding(building);

            await context.PostAsync(context.CreateMessage($"Building set to {building.BuildingName}.", InputHints.AcceptingInput));
            context.Done(building);
        }
    }
}
