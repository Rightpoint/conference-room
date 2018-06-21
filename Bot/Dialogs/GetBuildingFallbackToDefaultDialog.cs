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
    public class GetBuildingFallbackToDefaultDialog : IDialog<BuildingChoice>
    {
        private readonly string _buildingName;
        private Uri _requestUri;

        public GetBuildingFallbackToDefaultDialog(Uri requestUri, string buildingName)
        {
            _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
            _buildingName = buildingName;
        }

        public async Task StartAsync(IDialogContext context)
        {
            // without this wait, we'd double-process the last message recieved
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            if (!string.IsNullOrEmpty(_buildingName))
            {
                await context.Forward(new ChooseBuildingDialog(_requestUri, _buildingName), TryGotBuilding, context.Activity, new CancellationToken());
                return;
            }

            await context.Forward(new GetDefaultBuildingDialog(_requestUri), GotBuilding, context.Activity, new CancellationToken());
        }
        public async Task TryGotBuilding(IDialogContext context, IAwaitable<BuildingChoice> argument)
        {
            var building = await argument;

            if (string.IsNullOrEmpty(building?.BuildingId))
            {
                await context.Forward(new GetDefaultBuildingDialog(_requestUri), GotBuilding, context.Activity, new CancellationToken());
            }
            else
            {
                context.Done(building);
            }
        }

        public async Task GotBuilding(IDialogContext context, IAwaitable<BuildingChoice> argument)
        {
            context.Done(await argument);
        }
    }
}
