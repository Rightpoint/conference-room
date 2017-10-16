using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public class ResourceAuthTokenDialog : IDialog<string>
    {
        private readonly Uri _requestUri;
        private readonly string _resource;
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public ResourceAuthTokenDialog(Uri requestUri, string resource, bool ignoreCache, bool requireConsent)
        {
            _requestUri = requestUri;
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
            if (!_ignoreCache && !_requireConsent && context.UserData.TryGetValue("AuthToken_" + _resource, out accessToken) && !string.IsNullOrEmpty(accessToken))
            {
                context.Done(accessToken);
            }
            else
            {
                await context.Forward(new AppAuthTokenDialog(_requestUri, _ignoreCache, _requireConsent), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
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
            try
            {
                newToken = await authenticationContext.AcquireTokenAsync(_resource, clientCredential, userAssertion);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("AADSTS65001"))
                {
                    // consent required
                    await context.PostAsync("Looks like we haven't asked your consent for this, doing that now....");
                    await context.Forward(new AppAuthTokenDialog(_requestUri, true, true), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                if (ex.Message.Contains("AADSTS50013"))
                {
                    // invalid app access token
                    await context.PostAsync("Looks like your application token is expired - need a new one....");
                    await context.Forward(new AppAuthTokenDialog(_requestUri, true, false), RecieveAppAuthTokenAsync, context.Activity, new CancellationToken());
                    return;
                }
                throw;
            }
            
            if (!string.IsNullOrEmpty(newToken.AccessToken))
            {
                context.UserData.SetValue("AuthToken_" + _resource, newToken.AccessToken);
            }
            context.Done(newToken.AccessToken);
        }
    }
}