using System;
using System.Collections.Generic;

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
    }
}