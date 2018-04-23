using System;
using System.Configuration;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Azure;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Extensions;

namespace RightpointLabs.ConferenceRoom.Bot.Controllers
{
    [Route("/")]
    public class IndexController : Controller
    {
        [HttpGet]
        public async Task<object> Get()
        {
            return Content($"<html><head><title>Room Ninja Bot</title></head><body style='margin: 0; padding: 0;'><iframe style='height: 100%; width: 100%; margin: 0; padding: 0; border: 0;' src='https://webchat.botframework.com/embed/rproomsbot2?s={Config.GetAppSetting("BOT_WEB_SECRET")}'></iframe></body></html>", "text/html");
        }
    }
}
