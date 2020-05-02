using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beleriand.Extensions;
using Nito.AsyncEx;

namespace Beleriand.Core
{
    public abstract class CacheBase : ICache
    {
        public string Name { get; }

        protected readonly object SyncObj = new object();

        private readonly AsyncLock _asyncLock = new AsyncLock();

        public CacheBase(string name)
        {
            Name = name;
        }

        public abstract T GetOrDefault<T>(string key);

        public Task<T> GetOrDefaultAsync<T>(string key)
        {
            var localizedKey = GetLocalizedKey(key);

            return Task.FromResult(GetOrDefault<T>(localizedKey));
        }

        public abstract Dictionary<string, T> GetOrDefault<T>(List<string> keys);

        public Task<Dictionary<string, T>> GetOrDefaultAsync<T>(List<string> keys)
        {
            var localizedKeys = keys.Select(GetLocalizedKey).ToList();

            return Task.FromResult(GetOrDefault<T>(localizedKeys));
        }

        public T Get<T>(string key, Func<string, T> factory)
        {
            string cacheKey = key;
            T item = GetOrDefault<T>(key);
            if (item == null)
            {
                lock (SyncObj)
                {
                    item = GetOrDefault<T>(key);
                    if (item == null)
                    {
                        item = factory(key);
                        if (item == null)
                        {
                            return default(T);
                        }

                        Set(cacheKey, item);
                    }
                }
            }

            return item;
        }

        public async Task<T> GetAsync<T>(string key, Func<string, Task<T>> factory)
        {
            string cacheKey = key;
            T item = await GetOrDefaultAsync<T>(key);
            if (item == null)
            {
                using (await _asyncLock.LockAsync())
                {
                    item = await GetOrDefaultAsync<T>(key);
                    if (item == null)
                    {
                        item = await factory(key);
                        if (item == null)
                        {
                            return default(T);
                        }

                        await SetAsync(cacheKey, item);
                    }
                }
            }

            return item;
        }

        public Dictionary<string, T> Get<T>(List<string> keys, Func<List<string>, Dictionary<string, T>> factory)
        {
            Dictionary<string, T> itemMap = GetOrDefault<T>(keys);
            if (itemMap == null || itemMap.Count == 0)
            {
                lock (SyncObj)
                {
                    itemMap = GetOrDefault<T>(keys);

                    var notFoundKeys = keys.Where(a => !itemMap.Keys.Contains(a)).ToList();

                    if (notFoundKeys.Count != 0)
                    {
                        var notFoundItems = factory(notFoundKeys);

                        if (notFoundItems.Count != 0)
                        {
                            Set(notFoundItems);
                        }

                        itemMap.AddRange(notFoundItems);
                    }

                    if (itemMap.Count != keys.Count)
                    {
                        return default(Dictionary<string, T>);
                    }
                }
            }

            return itemMap;
        }

        public async Task<Dictionary<string, T>> GetAsync<T>(List<string> keys,
            Func<List<string>, Task<Dictionary<string, T>>> factory)
        {
            Dictionary<string, T> itemMap = await GetOrDefaultAsync<T>(keys);
            if (itemMap == null || itemMap.Count == 0)
            {
                using (await _asyncLock.LockAsync())
                {
                    itemMap = await GetOrDefaultAsync<T>(keys);

                    var notFoundKeys = keys.Where(a => !itemMap.Keys.Contains(a)).ToList();

                    if (notFoundKeys.Count != 0)
                    {
                        var notFoundItems = await factory(notFoundKeys);

                        if (notFoundItems.Count != 0)
                        {
                            await SetAsync<T>(notFoundItems);
                        }

                        itemMap.AddRange(notFoundItems);
                    }

                    if (itemMap.Count != keys.Count)
                    {
                        return default(Dictionary<string, T>);
                    }
                }
            }

            return itemMap;
        }

        public abstract void Set<T>(string key, T value);

        public Task SetAsync<T>(string key, T value)
        {
            var localizedKey = GetLocalizedKey(key);
            Set(localizedKey, value);
            return Task.FromResult(0);
        }

        public abstract void Set<T>(Dictionary<string, T> values);

        public Task SetAsync<T>(Dictionary<string, T> values)
        {
            Set(values);
            return Task.FromResult(0);
        }

        public abstract void Remove(string key);

        public Task RemoveAsync(string key)
        {
            var localizedKey = GetLocalizedKey(key);
            Remove(localizedKey);
            return Task.FromResult(0);
        }

        public abstract void Clear();

        public Task ClearAsync()
        {
            Clear();
            return Task.FromResult(0);
        }

        protected string GetLocalizedKey(string key)
        {
            return $"{Name}:{key}";
        }
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}