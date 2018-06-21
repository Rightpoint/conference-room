using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using RightpointLabs.BotLib;
using RightpointLabs.BotLib.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Controllers;
using RightpointLabs.ConferenceRoom.Bot.Extensions;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Dialogs
{
    [Serializable]
    public class RoomNinjaCustomTokenDialog : IDialog<string>
    {
        private readonly SecurityLevel _securityLevel;
        private readonly Uri _requestUri;
        private readonly string _resource;
        private readonly bool _ignoreCache;
        private readonly bool _requireConsent;

        public RoomNinjaCustomTokenDialog(IDialogContext context, Uri requestUri, string resource, bool ignoreCache, bool requireConsent)
        {
            _securityLevel = context.GetSecurityLevel();
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
            if (!_ignoreCache && context.UserData.TryGetValue(CacheKey, out accessToken) && !string.IsNullOrEmpty(accessToken))
            {
                Log($"RNCTD: using {accessToken}");
                context.Done(accessToken);
            }
            else
            {
                Log($"RNCTD: prompting");
                await context.Forward(CreateResourceAuthTokenDialog(), ReceiveTokenAsync, context.Activity, new CancellationToken());
            }
        }

        public async Task ReceiveTokenAsync(IDialogContext context, IAwaitable<string> awaitableArgument)
        {
            var accessToken = await awaitableArgument;

            if (!string.IsNullOrEmpty(accessToken))
            {
                if (_securityLevel == SecurityLevel.Low)
                {
                    Log($"RNCTD: using {accessToken} to get long-term access token");

                    try
                    {
                        using (var c = new HttpClient())
                        {
                            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                            var r = await c.PostAsync(new Uri(RoomsService.BaseUrl, "/api/tokens/getLongTerm"), new StringContent(""));
                            accessToken = await r.Content.ReadAsStringAsync();
                        }
                    }
                    catch
                    {
                        throw;
                    }

                    Log($"RNCTD: saving long-term token {accessToken}");
                    context.UserData.SetValue(CacheKey, accessToken);
                }
                else
                {
                    Log($"RNCTD: clearing any cached one and using short-term token {accessToken}");
                    context.UserData.SetValue(CacheKey, ""); // clear the token in the cache if there is one - we don't want to try using it
                }
            }
            context.Done(accessToken);
        }

        public string CacheKey => $"AuthToken_{this.GetType().Name}_{_securityLevel}";

        protected ResourceAuthTokenDialog CreateResourceAuthTokenDialog()
        {
            return new CustomResourceAuthTokenDialog(_requestUri, _resource, _ignoreCache, _requireConsent);
        }

        protected void Log(string message)
        {
            Trace.WriteLine(message);
            MessagesController.TelemetryClient.TrackTrace(message);
        }

        [Serializable]
        public class CustomResourceAuthTokenDialog : ResourceAuthTokenDialog
        {
            private readonly Uri _requestUri;

            public CustomResourceAuthTokenDialog(Uri requestUri, string resource, bool ignoreCache, bool requireConsent) : base(resource, ignoreCache, requireConsent)
            {
                _requestUri = requestUri;
            }

            protected override AppAuthTokenDialog CreateAppAuthTokenDialog(bool ignoreCache, bool requireConsent)
            {
                return new CustomAppAuthTokenDialog(_requestUri, ignoreCache, requireConsent);
            }

            protected override void Log(string message)
            {
                Trace.WriteLine(message);
                MessagesController.TelemetryClient.TrackTrace(message);
            }

            protected override void TokenRequestComplete(TimeSpan duration, Exception ex)
            {
                var now = DateTimeOffset.Now;
                MessagesController.TelemetryClient.TrackDependency("Http", "RequestToken", now.Subtract(duration), duration, null == ex);
            }
        }

        [Serializable]
        public class CustomAppAuthTokenDialog : AppAuthTokenDialog
        {
            private readonly Uri _requestUri;

            public CustomAppAuthTokenDialog(Uri requestUri, bool ignoreCache, bool requireConsent) : base(ignoreCache, requireConsent)
            {
                _requestUri = requestUri;
            }

            protected override LoginDialog CreateLoginDialog(bool requireConsent)
            {
                return new CustomLoginDialog(_requestUri, requireConsent);
            }

            protected override void Log(string message)
            {
                Trace.WriteLine(message);
                MessagesController.TelemetryClient.TrackTrace(message);
            }
        }

        [Serializable]
        public class CustomLoginDialog : LoginDialog
        {
            private readonly Uri _requestUri;

            public CustomLoginDialog(Uri requestUri, bool requireConsent) : base(requireConsent)
            {
                _requestUri = requestUri ?? throw new ArgumentNullException(nameof(requestUri));
            }

            protected override string GetRedirectUri()
            {
                return new Uri(_requestUri, "/api/Authorize").AbsoluteUri;
            }

            protected override async Task SaveSettings(IDialogContext context, SimpleAuthenticationResultModel authResult)
            {
                await base.SaveSettings(context, authResult);
                if (string.IsNullOrEmpty(authResult.GivenName) || string.IsNullOrEmpty(authResult.FamilyName))
                {
                    context.SetName(null);
                }
                else
                {
                    context.SetName($"{authResult.GivenName} {authResult.FamilyName}");
                }
                await this.PreAuthForResources(context, authResult, RoomsService.Resource);
            }

            protected override void Log(string message)
            {
                Trace.WriteLine(message);
                MessagesController.TelemetryClient.TrackTrace(message);
            }
        }
    }
}