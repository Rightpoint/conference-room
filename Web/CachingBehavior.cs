using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        private static readonly bool UnwrapTasks = true;

        private class CachedObject
        {
            public DateTime CacheTime { get; set; }
            public object Object { get; set; }
            public object UnwrappedTask { get; set; }
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
                                var ret = new CachedObject(DateTime.UtcNow, r?.ReturnValue, r?.Exception);
                                if (UnwrapTasks)
                                {
                                    if (ret.Object is Task && method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                                    {
                                        ((Task) ret.Object).ContinueWith(t =>
                                        {
                                            var tr = _taskResult.GetOrAdd(method.ReturnType.GetGenericArguments()[0], GetTaskResult);
                                            ret.UnwrappedTask = tr(t);
                                        });
                                    }
                                }
                                return ret;
                            });
                            if (result.CacheTime.AddMinutes(5) < DateTime.UtcNow)
                            {
                                cache.TryRemove(key2, out result);
                            }
                            else
                            {
                                return result.Exception != null
                                    ? input.CreateExceptionMethodReturn(result.Exception)
                                    : UnwrapTasks && result.UnwrappedTask != null && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                                        ? input.CreateMethodReturn(_taskFromResult.GetOrAdd(method.ReturnType.GetGenericArguments()[0], CreateTaskFromResult)(result.UnwrappedTask))
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

        private ConcurrentDictionary<Type, Func<Task,object>> _taskResult = new ConcurrentDictionary<Type, Func<Task, object>>();
        public Func<Task, object> GetTaskResult(Type type)
        {
            var _ = Expression.Parameter(typeof(Task), "_");
            var taskType = typeof(Task<>).MakeGenericType(type);
            var result = taskType.GetProperty("Result");
            var body = Expression.Convert(Expression.Property(Expression.Convert(_, taskType), result), typeof(object));
            return (Func<Task, object>)Expression.Lambda(body, _).Compile();
        }

        private ConcurrentDictionary<Type, Func<object, object>> _taskFromResult = new ConcurrentDictionary<Type, Func<object, object>>();
        public Func<object, object> CreateTaskFromResult(Type type)
        {
            var _ = Expression.Parameter(typeof(object), "_");
            var fromResult = typeof(Task).GetMethod("FromResult").MakeGenericMethod(type);
            var body = Expression.Convert(Expression.Call(fromResult, Expression.Convert(_, type)), typeof(object));
            return (Func<object, object>)Expression.Lambda(body, _).Compile();
        }

        public IEnumerable<Type> GetRequiredInterfaces()
        {
            return new Type[0];
        }

        public bool WillExecute { get; } = true;
    }
}
