using System.Threading.Tasks;
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

        public static async Task SendTyping(this IDialogContext context)
        {
            if (context.Activity is Activity activity)
            {
                var typing = activity.CreateReply();
                typing.Type = ActivityTypes.Typing;
                await context.PostAsync(typing);
            }
        }
    }
}
