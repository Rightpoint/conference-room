using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;

namespace RightpointLabs.ConferenceRoom.Bot.Extensions
{
    public static class MessageExtensions
    {
        public static IMessageActivity CreateMessage(this IDialogContext context, string text, string speak, string inputHint)
        {
            var msg = context.MakeMessage();

            if (context.Activity.ChannelId == "slack")
            {
                // let's try to respond to the thread....
                var channelData = GetActivityChannelData(context, new {SlackMessage = new {@event = new {ts = "", thread_ts = ""}}});
                var ts = channelData?.SlackMessage?.@event?.thread_ts;
                if (string.IsNullOrEmpty(ts))
                {
                    ts = channelData?.SlackMessage?.@event?.ts;
                }
                if (!string.IsNullOrEmpty(ts))
                {
                    msg.ChannelData = JObject.FromObject(new {thread_ts = ts});
                }
            }

            msg.Text = text;
            msg.Speak = speak;
            msg.InputHint = inputHint;
            return msg;
        }
        
        private static T GetActivityChannelData<T>(IDialogContext context, T prototype)
        {
            return context.Activity.GetChannelData<T>();
        }

        public static IMessageActivity CreateMessage(this IDialogContext context, string text, string inputHint)
        {
            return context.CreateMessage(text, text, inputHint);
        }
    }
}
