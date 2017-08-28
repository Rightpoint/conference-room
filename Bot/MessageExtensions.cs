using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace RightpointLabs.ConferenceRoom.Bot
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

        public static IMessageActivity CreateMessage(this IDialogContext context, string text, string inputHint)
        {
            return context.CreateMessage(text, text, inputHint);
        }
    }
}
