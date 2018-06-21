using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.ConferenceRoom.Bot.Extensions;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class ChooseBuildingDialog : IDialog<BuildingChoice>
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
                context.Done<BuildingChoice>(null);
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
                context.Done<BuildingChoice>(null);
            }
            else
            {
                var choice = new BuildingChoice()
                {
                    BuildingId = building.Id,
                    BuildingName = building.Name,
                    TimezoneId = GetTimezone(building.Id),
                };
                context.Done(choice);
            }
        }

        private string GetTimezone(string buildingId)
        {
            // TODO: load and cache this data from the building list
            switch (buildingId)
            {
                case "584f1a18c233813f98ef1513":
                case "584f1a22c233813f98ef1514":
                case "584f1a30c233813f98ef1517":
                    return "Eastern Standard Time";
                case "584f1a11c233813f98ef1512":
                case "584f1a26c233813f98ef1515":
                    return "Central Standard Time";
                case "584f1a2bc233813f98ef1516":
                    return "Mountain Standard Time";
                case "584f1a35c233813f98ef1518":
                    return "Pacific Standard Time";
            }
            return null;
        }
    }
}
