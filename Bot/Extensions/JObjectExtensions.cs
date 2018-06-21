using System;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json.Linq;
using RightpointLabs.BotLib;
using RightpointLabs.ConferenceRoom.Bot.Dialogs;
using RightpointLabs.ConferenceRoom.Bot.Models;
using RightpointLabs.ConferenceRoom.Bot.Services;

namespace RightpointLabs.ConferenceRoom.Bot.Extensions
{
    public static class JObjectExtensions
    {
        /// <summary>
        /// Clever way to call <see cref="JObject.ToObject{T}()"/> with an anonymous T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="prototype"></param>
        /// <returns></returns>
        public static T ToAnonymousObject<T>(this JObject obj, T prototype)
        {
            return obj.ToObject<T>();
        }
    }
}
