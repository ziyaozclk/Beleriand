using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Beleriand.Core
{
    public interface ICache : IDisposable
    {
        string Name { get; }

        T GetOrDefault<T>(string key);
        Task<T> GetOrDefaultAsync<T>(string key);

        List<T> GetOrDefault<T>(List<string> keys);
        Task<List<T>> GetOrDefaultAsync<T>(List<string> keys);

        T Get<T>(string key, Func<string, T> factory);
        Task<T> GetAsync<T>(string key, Func<string, Task<T>> factory);

        List<T> Get<T>(List<string> keys, Func<List<string>, List<T>> factory);
        Task<List<T>> GetAsync<T>(List<string> keys, Func<List<string>, Task<List<T>>> factory);

        void Set<T>(string key, T value);
        Task SetAsync<T>(string key, T value);

        void Set<T>(Dictionary<string, T> values);
        Task SetAsync<T>(Dictionary<string, T> values);

        void Remove(string key);
        Task RemoveAsync(string key);

        void Clear();
        Task ClearAsync();
    }
}