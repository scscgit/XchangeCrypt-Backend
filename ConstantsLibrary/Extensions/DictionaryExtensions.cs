using System;
using System.Collections.Generic;

namespace XchangeCrypt.Backend.ConstantsLibrary.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key)
        {
            return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue)
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TValue> defaultValueProvider)
        {
            return dictionary.TryGetValue(key, out var value)
                ? value
                : defaultValueProvider();
        }
    }
}
