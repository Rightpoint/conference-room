using System;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services.ExchangeEWS
{
    public interface IExchangeServiceManager
    {
        T Execute<T>(string targetUser, Func<ExchangeService, T> action);
        void Execute(string targetUser, Action<ExchangeService> action);
        T ExecutePrivate<T>(string targetUser, Func<ExchangeService, T> action);
        void ExecutePrivate(string targetUser, Action<ExchangeService> action);
    }
}