using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public abstract class RoomNinjaCallDialogBase<T> : AuthenticatedResourceActionDialogBase<T> where T:class
    {
        private readonly Uri _requestUri;

        protected RoomNinjaCallDialogBase(Uri requestUri)
        {
            _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
        }

        protected override string Resource => RoomsService.Resource;

        protected override async Task<T> DoWork(IDialogContext context, string accessToken)
        {
            return await DoWork(context, new RoomsService(accessToken));
        }

        protected abstract Task<T> DoWork(IDialogContext context, RoomsService accessToken);

        protected override IDialog<string> CreateResourceAuthTokenDialog(IDialogContext context, string resource, bool ignoreCache, bool requireConsent)
        {
            return new RoomNinjaCustomTokenDialog(context, _requestUri, resource, ignoreCache, requireConsent);
        }

        protected override void Log(string message)
        {
            Messages.CurrentLog.Info(message);
        }

    }
}