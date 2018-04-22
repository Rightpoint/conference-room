using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Extensions
{
    public static class HttpRequestExtensions
    {
        public static Uri GetRequestUri(this HttpRequest req)
        {
            return new UriBuilder
            {
                Scheme = req.Scheme,
                Host = req.Host.Host,
                Port = req.Host.Port.GetValueOrDefault(80),
                Path = req.Path.ToString(),
                Query = req.QueryString.ToString()
            }.Uri;
        }
    }
}
