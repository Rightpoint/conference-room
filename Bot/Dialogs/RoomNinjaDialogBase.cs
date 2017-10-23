using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    public abstract class RoomNinjaDialogBase<T> : AuthenticatedResourceActionDialogBase<T> where T:class
    {
        public RoomNinjaDialogBase(Uri requestUri) : base(requestUri)
        {
        }

        protected override string Resource => "https://roomninja.rightpoint.com/";

        protected override async Task DoWork(IDialogContext context, string accessToken)
        {
            await DoWork(context, new RoomsService(accessToken));
        }

        protected abstract Task DoWork(IDialogContext context, RoomsService accessToken);
    }
}
