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
        private readonly Uri _requestUri;
        protected abstract string Resource { get; }
        protected virtual string NoAccessTokenMessage => "You need to authenticate for me to do that";

        protected AuthenticatedResourceActionDialogBase(Uri requestUri)
        {
            _requestUri = requestUri;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            await context.Forward(new ResourceAuthTokenDialog(_requestUri, Resource, false, false), ResumeProcess, context.Activity, new CancellationToken());
        }
        
        public async Task ResumeProcess(IDialogContext context, IAwaitable<string> accessTokenAwaitable)
        {
            var accessToken = await accessTokenAwaitable;

            if (string.IsNullOrEmpty(accessToken))
            {
                await context.PostAsync(NoAccessTokenMessage);
                context.Done(DoneObject);
            }
            else
            {
                try
                {
                    await DoWork(context, accessToken);
                    context.Done(DoneObject);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.Message.Contains("401 (Unauthorized)"))
                    {
                        await context.PostAsync("Looks like your resource token is expired - need a new one....");
                        await context.Forward(new ResourceAuthTokenDialog(_requestUri, Resource, true, false), ResumeProcess, context.Activity, new CancellationToken());
                        return;
                    }
                    throw;
                }
                catch (WebException ex)
                {
                    if (ex.Message.Contains("(401) Unauthorized"))
                    {
                        await context.PostAsync("Looks like your resource token is expired - need a new one....");
                        await context.Forward(new ResourceAuthTokenDialog(_requestUri, Resource, true, false), ResumeProcess, context.Activity, new CancellationToken());
                        return;
                    }
                    throw;
                }
            }
        }

        protected abstract Task DoWork(IDialogContext context, string accessToken);

        protected virtual T DoneObject => string.Empty as T;
    }
}