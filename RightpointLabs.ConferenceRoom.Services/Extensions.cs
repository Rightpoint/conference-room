using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RightpointLabs.ConferenceRoom.Services
{
    public static class Extensions
    {
        /// <summary>
        /// Similar to <see cref="IDictionary&lt;K,T&gt;.TryGetValue(K, out T)"/>, but returns the value from
        ///   the dictionary instead of using an output parameter.
        /// Returns default(T) if the key does not exist.  This means you cannot tell the difference between a
        ///   default value and a non-existant key.  Do not use this function if you care about the difference.
        /// 
        /// This exists partially to allow a nested dictionary structure to be easily traversed via <see cref="Extensions.ChainIfNotNull{T,T}"/>
        /// </summary>
        /// <typeparam name="K">The key type of the dictionary</typeparam>
        /// <typeparam name="T">The value type of the dictionary</typeparam>
        /// <param name="dictionary">The dictionary</param>
        /// <param name="key">The key</param>
        /// <returns>The value in the dictionary for the passed key, or default(T) if the key doesn't exist.</returns>
        public static T TryGetValue<K, T>(this IDictionary<K, T> dictionary, K key)
        {
            T value;
            if (dictionary.TryGetValue(key, out value))
            {
                return value;
            }
            return default(T);
        }

        /// <summary>
        /// If obj is null, returns null immediately.  Otherwises, passes obj to chainCall and returns the result.
        /// 
        /// This essentially lets us fake null propagation.  Description: http://kontrawize.blogs.com/kontrawize/2007/03/null_propagatio.html.
        /// </summary>
        /// <typeparam name="T1">The type of the input object</typeparam>
        /// <typeparam name="T2">The return type</typeparam>
        /// <param name="obj">The object to be checked for null and passed to chainCall</param>
        /// <param name="chainCall">The delegate to be called with obj as an argument</param>
        /// <returns>null if obj is null, or the result of calling chainCall with obj as an argument.</returns>
        public static T2 ChainIfNotNull<T1, T2>(this T1 obj, Func<T1, T2> chainCall) where T1 : class
        {
            if (null == obj)
                return default(T2);
            return chainCall(obj);
        }

    }
}