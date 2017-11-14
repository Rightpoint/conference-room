using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public abstract class ResourceAuthTokenDialog : IDialog<string>
    {
        private readonly string _resource;
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public ResourceAuthTokenDialog(string resource, bool ignoreCache, bool requireConsent)
        {
            _resource = resource;
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
                Log($"RATD: using {accessToken}");
                context.Done(accessToken);
            }
            else
            {
                Log($"RATD: prompting for token");
                await context.Forward(CreateAppAuthTokenDialog(_ignoreCache, _requireConsent), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
            }
        }

        public async Task RecieveAppAuthTokenAsync(IDialogContext context, IAwaitable<string> awaitableArgument)
        {
            var appAccessToken = await awaitableArgument;

            var idaClientId = ConfigurationManager.AppSettings["ClientId"];
            var idaClientSecret = ConfigurationManager.AppSettings["ClientSecret"];

            var clientCredential = new ClientCredential(idaClientId, idaClientSecret);
            var userAssertion = new UserAssertion(appAccessToken);
            var authenticationContext = new AuthenticationContext(ConfigurationManager.AppSettings["Authority"]);

            AuthenticationResult newToken;
            var sw = Stopwatch.StartNew();
            try
            {
                Log($"RATD: redeeming for token");
                newToken = await authenticationContext.AcquireTokenAsync(_resource, clientCredential, userAssertion);
                TokenRequestComplete(sw.Elapsed, null);
            }
            catch (Exception ex)
            {
                TokenRequestComplete(sw.Elapsed, ex);
                if (ex.Message.Contains("AADSTS65001"))
                {
                    // consent required
                    Log($"RATD: need consent");
                    await context.PostAsync("Looks like we haven't asked your consent for this, doing that now....");
                    await context.Forward(CreateAppAuthTokenDialog(true, true), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                if (ex.Message.Contains("AADSTS50013"))
                {
                    // invalid app access token
                    Log($"RATD: token expired");
                    await context.PostAsync("Looks like your application token is expired - need a new one....");
                    await context.Forward(CreateAppAuthTokenDialog(true, false), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                throw;
            }
            
            if (!string.IsNullOrEmpty(newToken.AccessToken))
            {
                Log($"RATD: saving token");
                context.UserData.SetValue(CacheKey, newToken.AccessToken);
            }
            context.Done(newToken.AccessToken);
        }

        protected string CacheKey => $"AuthToken_{this.GetType().Name}_{_resource}";

        protected abstract AppAuthTokenDialog CreateAppAuthTokenDialog(bool ignoreCache, bool requireConsent);

        protected virtual void Log(string message)
        {
        }

        protected virtual void TokenRequestComplete(TimeSpan duration, Exception ex)
        {
        }
    }
}