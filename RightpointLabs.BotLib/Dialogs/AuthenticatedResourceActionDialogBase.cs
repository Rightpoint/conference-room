using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public abstract class AuthenticatedResourceActionDialogBase<T> : IDialog<T> where T:class
    {
        protected abstract string Resource { get; }
        protected virtual string NoAccessTokenMessage => "You need to authenticate for me to do that";

        protected AuthenticatedResourceActionDialogBase()
        {
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await context.Forward(CreateResourceAuthTokenDialog(context, Resource, false, false), ResumeProcess, context.Activity, new CancellationToken());
        }
        
        public async Task ResumeProcess(IDialogContext context, IAwaitable<string> accessTokenAwaitable)
        {
            var accessToken = await accessTokenAwaitable;

            if (string.IsNullOrEmpty(accessToken))
            {
                Log($"ARAD: got no token");
                await context.PostAsync(NoAccessTokenMessage);
                context.Done(ErrorDoneObject);
            }
            else
            {
                try
                {
                    var result = await DoWork(context, accessToken);
                    context.Done(result);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains("401 (Unauthorized)"))
                    {
                        Log($"ARAD: expired token - HRE");
                        await context.PostAsync("Looks like your resource token is expired - need a new one....");
                        await context.Forward(CreateResourceAuthTokenDialog(context, Resource, true, false), ResumeProcess, context.Activity, new CancellationToken());
                        return;
                    }
                    throw;
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("(401) Unauthorized"))
                    {
                        Log($"ARAD: expired token - WE");
                        await context.PostAsync("Looks like your resource token is expired - need a new one....");
                        await context.Forward(CreateResourceAuthTokenDialog(context, Resource, true, false), ResumeProcess, context.Activity, new CancellationToken());
                        return;
                    }
                    throw;
                }
            }
        }

        protected abstract Task<T> DoWork(IDialogContext context, string accessToken);

        protected virtual T ErrorDoneObject => string.Empty as T;

        protected abstract IDialog<string> CreateResourceAuthTokenDialog(IDialogContext context, string resource, bool ignoreCache, bool requireConsent);

        protected virtual void Log(string message)
        {
        }
    }
}