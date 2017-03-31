using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;
using Microsoft.Practices.Unity.InterceptionExtension;

namespace RightpointLabs.ConferenceRoom.Web
{
    public class CachingBehavior: IInterceptionBehavior
    {
        private static ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ConcurrentDictionary<string, CachedObject> _cache = new ConcurrentDictionary<string, CachedObject>();

        private class CachedObject
        {
            public DateTime CacheTime { get; set; }
            public object Object { get; set; }
            public Exception Exception { get; set; }

            public CachedObject(DateTime cacheTime, object o, Exception exception)
            {
                CacheTime = cacheTime;
                Object = o;
                Exception = exception;
            }
        }

        public IMethodReturn Invoke(IMethodInvocation input, GetNextInterceptionBehaviorDelegate getNext)
        {
            if (input.MethodBase is MethodInfo method)
            {
                if (method.Name.StartsWith("Get"))
                {
                    var parameters = method.GetParameters();
                    if (parameters.All(i => i.ParameterType == typeof(string)))
                    {
                        var key = $"{method.ReflectedType.FullName}_{method.Name}_{parameters.Length}_" + string.Join("_", input.Inputs.Cast<object>());
                        log.Debug($"Using key {key}");

                        CachedObject result;
                        while(true)
                        {
                            result = _cache.GetOrAdd(key, _ =>
                            {
                                log.Debug($"Cache miss on {key}");
                                var r = getNext()(input, getNext);
                                return new CachedObject(DateTime.UtcNow, r?.ReturnValue, r?.Exception);
                            });
                            if (result.CacheTime.AddMinutes(5) < DateTime.UtcNow)
                            {
                                _cache.TryRemove(key, out result);
                            }
                            else
                            {
                                return result.Exception != null ? 
                                    input.CreateExceptionMethodReturn(result.Exception) : 
                                    input.CreateMethodReturn(result.Object);
                            }
                        }
                    }
                }
            }
            
            log.Debug($"Not caching call to {input.MethodBase}");
            return getNext()(input, getNext);
        }

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return new Type[0];
        }

        public bool WillExecute { get; } = true;
    }
}
