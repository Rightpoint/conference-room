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

        public RoomNinjaCallDialogBase(Uri requestUri)
        {
            _requestUri = requestUri;
        }

        protected override string Resource => RoomsService.Resource;

        protected override async Task<T> DoWork(IDialogContext context, string accessToken)
        {
            //// TODO: rework this so we cache the result token here, instead of the input one...
            //try
            //{
            //    using (var c = new HttpClient())
            //    {
            //        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            //        var r = await c.PostAsync(new Uri(RoomsService.BaseUrl, "/api/tokens/getCustom"), new StringContent(""));
            //        var token = await r.Content.ReadAsStringAsync();
            //        return await DoWork(context, new RoomsService(token));
            //    }
            //}
            //catch
            //{
            //    throw;
            //}

            // actually, think we can use the Azure AD resource token directly....
            return await DoWork(context, new RoomsService(accessToken));
        }

        protected abstract Task<T> DoWork(IDialogContext context, RoomsService accessToken);

        protected override ResourceAuthTokenDialog CreateResourceAuthTokenDialog(string resource, bool ignoreCache, bool requireConsent)
        {
            return new CustomResourceAuthTokenDialog(_requestUri, resource, ignoreCache, requireConsent);
        }

        protected override void Log(string message)
        {
            Messages.CurrentLog.Info(message);
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
                Messages.CurrentLog.Info(message);
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
                Messages.CurrentLog.Info(message);
            }
        }

        [Serializable]
        public class CustomLoginDialog : LoginDialog
        {
            private readonly Uri _requestUri;

            public CustomLoginDialog(Uri requestUri, bool requireConsent) : base(requireConsent)
            {
                _requestUri = requestUri;
            }

            protected override string GetRedirectUri()
            {
                return new Uri(_requestUri, "/api/Authorize").AbsoluteUri;
            }

            protected override void Log(string message)
            {
                Messages.CurrentLog.Info(message);
            }
        }
    }
}