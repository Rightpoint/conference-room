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

        protected TimeZoneInfo GetTimezone(RoomSearchCriteria.OfficeOptions office)
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
