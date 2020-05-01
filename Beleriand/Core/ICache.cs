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

        Dictionary<string,T> GetOrDefault<T>(List<string> keys);
        Task<Dictionary<string,T>> GetOrDefaultAsync<T>(List<string> keys);

        T Get<T>(string key, Func<string, T> factory);
        Task<T> GetAsync<T>(string key, Func<string, Task<T>> factory);

        Dictionary<string,T> Get<T>(List<string> keys, Func<List<string>,Dictionary<string,T>> factory);
        Task<Dictionary<string,T>> GetAsync<T>(List<string> keys, Func<List<string>, Task<Dictionary<string,T>>> factory);

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