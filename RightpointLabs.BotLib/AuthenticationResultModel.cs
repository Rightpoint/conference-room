using System;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib
{
    public class AuthenticationResultModel : BaseActivity
    {
        public string AccessToken { get; set; }
        public string SecurityKey { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }

        public AuthenticationResultModel(IActivity innerActivity) : base(innerActivity)
        {
        }

        public SimpleAuthenticationResultModel ToSimpleAuthenticationResultModel()
        {
            return new SimpleAuthenticationResultModel
            {
                AccessToken = this.AccessToken,
                SecurityKey = this.SecurityKey,
                Error = this.Error,
                ErrorDescription = this.ErrorDescription,
            };
        }
    }

    [Serializable]
    public class SimpleAuthenticationResultModel
    {
        public string AccessToken { get; set; }
        public string SecurityKey { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }
    }

    public class BaseActivity : IActivity
    {
        public BaseActivity(IActivity other)
        {
            this.Type = other.Type;
            this.Id = other.Id;
            this.ServiceUrl = other.ServiceUrl;
            this.Timestamp = other.Timestamp;
            this.LocalTimestamp = other.LocalTimestamp;
            this.ChannelId = other.ChannelId;
            //this.From = other.From;
            //this.Conversation = other.Conversation;
            //this.Recipient = other.Recipient;
            this.ReplyToId = other.ReplyToId;
            //this.ChannelData = other.ChannelData;
        }

        public IMessageActivity AsMessageActivity()
        {
            return null;
        }

        public IContactRelationUpdateActivity AsContactRelationUpdateActivity()
        {
            return null;
        }

        public IInstallationUpdateActivity AsInstallationUpdateActivity()
        {
            return null;
        }

        public IConversationUpdateActivity AsConversationUpdateActivity()
        {
            return null;
        }

        public ITypingActivity AsTypingActivity()
        {
            return null;
        }

        public IEndOfConversationActivity AsEndOfConversationActivity()
        {
            return null;
        }

        public IEventActivity AsEventActivity()
        {
            return null;
        }

        public IInvokeActivity AsInvokeActivity()
        {
            return null;
        }

        public string Type { get; set; }
        public string Id { get; set; }
        public string ServiceUrl { get; set; }
        public DateTime? Timestamp { get; set; }
        public DateTimeOffset? LocalTimestamp { get; set; }
        public string ChannelId { get; set; }
        public ChannelAccount From { get; set; }
        public ConversationAccount Conversation { get; set; }
        public ChannelAccount Recipient { get; set; }
        public string ReplyToId { get; set; }
        public dynamic ChannelData { get; set; }
    }
}