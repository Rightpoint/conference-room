using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public class AppAuthTokenDialog : IDialog<string>
    {
        private readonly Uri _requestUri;
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public AppAuthTokenDialog(Uri requestUri, bool ignoreCache, bool requireConsent)
        {
            _requestUri = requestUri;
            _ignoreCache = ignoreCache;
            _requireConsent = requireConsent;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            string accessToken;
            if (!_ignoreCache && !_requireConsent && context.UserData.TryGetValue("AuthToken", out accessToken) && !string.IsNullOrEmpty(accessToken))
            {
                context.Done(accessToken);
            }
            else
            {
                await context.Forward(new LoginDialog(_requestUri, _requireConsent), ReceiveTokenAsync, context.Activity, new CancellationToken());
            }
        }

        public async Task ReceiveTokenAsync(IDialogContext context, IAwaitable<string> awaitableArgument)
        {
            var accessToken = await awaitableArgument;
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.UserData.SetValue("AuthToken", accessToken);
            }
            context.Done(accessToken);
        }
    }
}