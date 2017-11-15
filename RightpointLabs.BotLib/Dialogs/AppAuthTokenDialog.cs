using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public abstract class AppAuthTokenDialog : IDialog<string>
    {
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public AppAuthTokenDialog(bool ignoreCache, bool requireConsent)
        {
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
            if (!_ignoreCache && !_requireConsent && context.UserData.TryGetValue(CacheKey, out accessToken) && !string.IsNullOrEmpty(accessToken))
            {
                Log($"AATD: using {accessToken}");
                context.Done(accessToken);
            }
            else
            {
                Log($"AATD: prompting");
                await context.Forward(CreateLoginDialog(_requireConsent), ReceiveTokenAsync, context.Activity, new CancellationToken());
            }
        }

        public async Task ReceiveTokenAsync(IDialogContext context, IAwaitable<string> awaitableArgument)
        {
            var accessToken = await awaitableArgument;
            if (!string.IsNullOrEmpty(accessToken))
            {
                Log($"AATD: saving {accessToken}");
                context.UserData.SetValue(CacheKey, accessToken);
            }
            context.Done(accessToken);
        }

        public string CacheKey => $"AuthToken_{this.GetType().Name}";

        protected abstract LoginDialog CreateLoginDialog(bool requireConsent);

        protected virtual void Log(string message)
        {
        }
    }
}