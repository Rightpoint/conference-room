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
        private ConcurrentDictionary<string, ConcurrentDictionary<string, CachedObject>> _cache = new ConcurrentDictionary<string, ConcurrentDictionary<string, CachedObject>>();

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
                        var key1 = method.ReflectedType.FullName;
                        var key2 = $"{method.Name}_{parameters.Length}_" + string.Join("_", input.Inputs.Cast<object>());
                        log.Debug($"Using keys {key1} {key2}");

                        CachedObject result;
                        while (true)
                        {
                            var cache = _cache.GetOrAdd(key1, new ConcurrentDictionary<string, CachedObject>());
                            result = cache.GetOrAdd(key2, _ =>
                            {
                                log.Debug($"Cache miss on {key2}");
                                var r = getNext()(input, getNext);
                                return new CachedObject(DateTime.UtcNow, r?.ReturnValue, r?.Exception);
                            });
                            if (result.CacheTime.AddMinutes(5) < DateTime.UtcNow)
                            {
                                cache.TryRemove(key2, out result);
                            }
                            else
                            {
                                return result.Exception != null
                                    ? input.CreateExceptionMethodReturn(result.Exception)
                                    : input.CreateMethodReturn(result.Object);
                            }
                        }
                    }
                }
                else
                {
                    // hmm.... we better clear the cache for this object to be sure....
                    var key1 = method.ReflectedType.FullName;
                    log.Debug("Clearing cache for {key1}");
                    ConcurrentDictionary<string, CachedObject> removed;
                    _cache.TryRemove(key1, out removed);
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
