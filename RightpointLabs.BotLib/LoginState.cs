using Microsoft.Bot.Builder.Dialogs;

namespace RightpointLabs.BotLib
{
    public class LoginState
    {
        public ResumptionCookie State { get; set; }
        public string LastUpn { get; set; }
    }
}