using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class ChooseBuildingDialog : IDialog<string>
    {
        private Uri _requestUri;
        private string _buildingName;

        public ChooseBuildingDialog(Uri requestUri, string buildingName)
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
            if (null == _buildingName)
            {
                await context.PostAsync(context.CreateMessage($"What building are you in?", InputHints.ExpectingInput));
                context.Wait(GetBuildingName);
                return;
            }

            await context.Forward(new RoomNinjaGetBuildingsCallDialog(_requestUri), GotBuildings, context.Activity, new CancellationToken());
        }

        public async Task GetBuildingName(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            _buildingName = (await argument).Text;
            if (null == _buildingName)
            {
                context.Done(string.Empty);
                return;
            }

            await context.Forward(new RoomNinjaGetBuildingsCallDialog(_requestUri), GotBuildings, context.Activity, new CancellationToken());
        }

        private async Task GotBuildings(IDialogContext context, IAwaitable<RoomsService.BuildingResult[]> callback)
        {
            var building = (await callback).MatchName(_buildingName);
            if (null == building)
            {
                await context.PostAsync(context.CreateMessage($"Can't find building {_buildingName}.", InputHints.AcceptingInput));
                context.Done(string.Empty);
            }
            else
            {
                context.Done(building.Id);
            }
        }
    }
}
