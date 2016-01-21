using System;
using System.Collections.Concurrent;
using System.Reflection;
using log4net;
using Microsoft.Exchange.WebServices.Data;

namespace RightpointLabs.ConferenceRoom.Infrastructure.Services
{
    public class ExchangeServiceManager : IExchangeServiceManager
    {
        private static ILog log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly Func<ExchangeService> _connectionBuilder;
        private readonly ConcurrentDictionary<string, ManagedConnection> _connections = new ConcurrentDictionary<string, ManagedConnection>();

        public ExchangeServiceManager(Func<ExchangeService> connectionBuilder)
        {
            _connectionBuilder = connectionBuilder;
        }

        public T Execute<T>(string targetUser, Func<ExchangeService, T> action)
        {
            targetUser = targetUser ?? "";
            return _connections.GetOrAdd(targetUser, x =>
            {
                log.DebugFormat("Creating new connection for {0}", targetUser);
                var cn = _connectionBuilder();
                if (!string.IsNullOrEmpty(targetUser))
                {
                    cn.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, targetUser);
                }
                return new ManagedConnection(cn);
            }).Execute(action);
        }

        public void Execute(string targetUser, Action<ExchangeService> action)
        {
            Execute(targetUser, svc =>
            {
                action(svc);
                return 0;
            });
        }

        private class ManagedConnection
        {
            private readonly BlockingCollection<ExchangeService> _queue;

            public ManagedConnection(ExchangeService connection)
            {
                _queue = new BlockingCollection<ExchangeService>();
                _queue.Add(connection);
            }

            public T Execute<T>(Func<ExchangeService, T> action)
            {
                var cn = _queue.Take();
                try
                {
                    // add connection timeout/retry stuff here if necessary
                    return action(cn);
                }
                finally
                {
                    _queue.Add(cn);
                }
            }
        }
    }
}
