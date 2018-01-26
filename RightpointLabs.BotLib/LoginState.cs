using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib
{
    public class LoginState
    {
        public ConversationReference State { get; set; }
        public string LastUpn { get; set; }
    }
}