using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib.Dialogs
{
    [Serializable]
    public abstract class LoginDialog : IDialog<string>
    {
        private readonly bool _requireConsent;
        private SimpleAuthenticationResultModel _authResult;
        private bool _promptForKey = true;

        public LoginDialog(bool requireConsent)
        {
            _requireConsent = requireConsent;
        }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument;

            // need to ask the user to authenticate
            var replyToConversation = context.MakeMessage();
            replyToConversation.Recipient = activity.From;
            replyToConversation.Type = "message";

            var signinButton = new CardAction()
            {
                Value = (await GetAuthUrl(context, activity)).AbsoluteUri,
                Type = (activity.ChannelId == "msteams" || activity.ChannelId == "cortana") ? "openUrl" : "signin",
                Title = "Authentication Required"
            };
            var signinCard =
                (activity.ChannelId == "msteams" || activity.ChannelId == "cortana") ? 
                new ThumbnailCard("Please login to this bot", null, null, null, new List<CardAction>() { signinButton }).ToAttachment() : 
                new SigninCard("Please login to this bot", new List<CardAction>() { signinButton }).ToAttachment();
            replyToConversation.Attachments = new List<Attachment>() { signinCard };

            _promptForKey = activity.ChannelId != "cortana";

            Log($"LD: Prompting for login from {activity.From.Id}/{activity.From.Name} on {activity.ChannelId}");
            await context.PostAsync(replyToConversation);

            context.Wait<object>(ReceiveTokenAsync);
        }

        private async Task<Uri> GetAuthUrl(IDialogContext context, IMessageActivity activity)
        {
            var authority = ConfigurationManager.AppSettings["Authority"];
            var p = new Dictionary<string, string>();
            p["client_id"] = ConfigurationManager.AppSettings["ClientId"];
            p["redirect_uri"] = GetRedirectUri();
            p["response_mode"] = "form_post";
            p["response_type"] = "code";
            p["scope"] = "openid profile";
            if (_requireConsent)
            {
                p["prompt"] = "consent";
            }
            p["state"] = SecureUrlToken.Encode(new LoginState() { State = new ResumptionCookie(activity), LastUpn = context.UserData.TryGetValue(nameof(LoginState.LastUpn), out string value) ? value : null });

            return new Uri(authority + "/oauth2/authorize?" + string.Join("&", p.Select(i => $"{HttpUtility.UrlEncode(i.Key)}={HttpUtility.UrlEncode(i.Value)}")));
        }

        public async Task ReceiveTokenAsync(IDialogContext context, IAwaitable<object> awaitableArgument)
        {
            var argument = await awaitableArgument;
            var result = argument as AuthenticationResultModel;

            if (argument is IMessageActivity)
            {
                await context.PostAsync("Cancelled");
                context.Done(string.Empty);
                return;
            }
            else if (null != result)
            {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Log($"LD: {result.Error}: {result.ErrorDescription}");
                    await context.PostAsync($"{result.Error}: {result.ErrorDescription}");
                    context.Done(string.Empty);
                }
                if (string.IsNullOrEmpty(result.SecurityKey))
                {
                    Log($"LD: got token, no key needed");
                    await context.PostAsync("Got your token, no security key is required");
                    SaveSettings(context, result.ToSimpleAuthenticationResultModel());
                    context.Done(result.AccessToken);
                }
                else
                {
                    Log($"LD: got token, waiting for key");
                    _authResult = result.ToSimpleAuthenticationResultModel();
                    if(_promptForKey)
                        await context.PostAsync("Please enter your security key");
                    context.Wait(ReceiveSecurityKeyAsync);
                }
                return;
            }

            await context.PostAsync("Got unknown thing: " + argument?.GetType()?.Name);
            context.Wait<object>(ReceiveTokenAsync);
        }

        public async Task ReceiveSecurityKeyAsync(IDialogContext context, IAwaitable<IMessageActivity> awaitableArgument)
        {
            var message = await awaitableArgument;
            var securityKeyRegex = new Regex("[^0-9]");

            if (message.Text.ToLower() == "cancel")
            {
                context.Done<string>(null);
            }
            else if (message.Text.ToLower() == "retry")
            {
                await MessageReceivedAsync(context, awaitableArgument);
            }
            else if (_authResult.SecurityKey == securityKeyRegex.Replace(message.Text, ""))
            {
                Log($"LD: security key matches");
                await context.PostAsync("Security key matches");
                SaveSettings(context, _authResult);
                context.Done(_authResult.AccessToken);
            }
            else
            {
                await context.PostAsync("Sorry, I didn't understand you.  Enter your security key, or 'cancel' to abort, or 'retry' to get a new authentication link.");
                context.Wait(ReceiveSecurityKeyAsync);
            }
        }

        protected virtual void SaveSettings(IDialogContext context, SimpleAuthenticationResultModel authResult)
        {
            if (string.IsNullOrEmpty(authResult.Upn))
            {
                context.UserData.RemoveValue(nameof(LoginState.LastUpn));
            }
            else
            {
                context.UserData.SetValue(nameof(LoginState.LastUpn), authResult.Upn);
            }
        }

        protected abstract string GetRedirectUri();

        protected virtual void Log(string message)
        {
        }
    }
}