using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public abstract class RoomNinjaDialogBase : IDialog<string>
    {
        protected Uri _requestUri;

        public abstract Task StartAsync(IDialogContext context);

        protected async Task BookIt(IDialogContext context, string roomId, DateTimeOffset? criteriaStartTime, DateTimeOffset? criteriaEndTime)
        {
            if (!criteriaStartTime.HasValue || !criteriaEndTime.HasValue)
            {
                throw new ApplicationException("Start and end time are required to schedule a meeting");
            }
            await context.Forward(new RoomNinjaScheduleMeetingCallDialog(_requestUri, roomId, criteriaStartTime.Value, criteriaEndTime.Value), BookedIt, context.Activity, new CancellationToken());
        }

        private async Task BookedIt(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var msg = await result;
                await context.PostAsync(context.CreateMessage(msg, InputHints.AcceptingInput));
            }
            catch (ApplicationException ex)
            {
                await context.PostAsync(context.CreateMessage($"Failed to book room: {ex.Message}", InputHints.AcceptingInput));
            }
            context.Done(string.Empty);
        }

        protected TimeZoneInfo GetTimezone(string buildingId)
        {
            // TODO: load and cache this data from the building list
            switch (buildingId)
            {
                case "584f1a18c233813f98ef1513":
                case "584f1a22c233813f98ef1514":
                case "584f1a30c233813f98ef1517":
                    return TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                case "584f1a11c233813f98ef1512":
                case "584f1a26c233813f98ef1515":
                    return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                case "584f1a2bc233813f98ef1516":
                    return TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
                case "584f1a35c233813f98ef1518":
                    return TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            return null;
        }
    }
}
