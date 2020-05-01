using System;
using System.Collections.Generic;
using System.Linq;

namespace Beleriand.Extensions
{
    public static class DictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> values, IEnumerable<KeyValuePair<TKey, TValue>> other)
        {
            foreach (var kvp in other)
            {
                if (values.ContainsKey(kvp.Key))
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }
                values.Add(kvp);
            }
        }

        public static void Merge(this IDictionary<string, object> instance, string key, object value, bool replaceExisting = true)
        {
            if (replaceExisting || !instance.ContainsKey(key))
            {
                instance[key] = value;
            }
        }

        public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> instance, IDictionary<TKey, TValue> from, bool replaceExisting = true)
        {
            foreach (var kvp in from)
            {
                if (replaceExisting || !instance.ContainsKey(kvp.Key))
                {
                    instance[kvp.Key] = kvp.Value;
                }
            }
        }

        public static void AppendInValue(this IDictionary<string, object> instance, string key, string separator, object value)
        {
            AddInValue(instance, key, separator, value, false);
        }

        public static void PrependInValue(this IDictionary<string, object> instance, string key, string separator, object value)
        {
            AddInValue(instance, key, separator, value, true);
        }

        private static void AddInValue(IDictionary<string, object> instance, string key, string separator, object value, bool prepend = false)
        {
            var valueStr = value.ToString();

            if (!instance.ContainsKey(key))
            {
                instance[key] = valueStr;
            }
            else
            {
                var arr = instance[key].ToString().Trim().Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable();
                var arrValue = valueStr.Trim().Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries).AsEnumerable();

                arr = prepend ? arrValue.Union(arr) : arr.Union(arrValue);

                instance[key] = string.Join(separator, arr);
            }
        }

        /*
        public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> instance, TKey key)
        {
            Guard.NotNull(instance, nameof(instance));

            instance.TryGetValue(key, out var val);
            return val;
        }

        public static ExpandoObject ToExpandoObject(this IDictionary<string, object> source, bool castIfPossible = false)
        {
            Guard.NotNull(source, nameof(source));

            if (castIfPossible && source is ExpandoObject)
            {
                return source as ExpandoObject;
            }

            var result = new ExpandoObject();
            result.AddRange(source);

            return result;
        }

        internal static bool TryGetValue<T>(this IDictionary<string, object> dictionary, string key, out T value)
        {
            object valueObj;
            if (dictionary.TryGetValue(key, out valueObj) && valueObj is T)
            {
                value = (T)valueObj;
                return true;
            }

            value = default(T);
            return false;
        }

        public static TValue GetOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue obj;
            return dictionary.TryGetValue(key, out obj) ? obj : default(TValue);
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
        {
            TValue obj;
            if (dictionary.TryGetValue(key, out obj))
            {
                return obj;
            }

            return dictionary[key] = factory(key);
        }

        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> factory)
        {
            return dictionary.GetOrAdd(key, k => factory());
        }
        */
    }
}