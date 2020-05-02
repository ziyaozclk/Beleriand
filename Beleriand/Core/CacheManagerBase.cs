using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Beleriand.Core.Configurations;

namespace Beleriand.Core
{
    public abstract class CacheManagerBase : ICacheManager
    {
        protected readonly ICachingConfiguration Configuration;
        protected readonly ConcurrentDictionary<string, CacheBase> Caches;

        public CacheManagerBase(ICachingConfiguration configuration)
        {
            Configuration = configuration;
            Caches = new ConcurrentDictionary<string, CacheBase>();
        }

        public IReadOnlyList<CacheBase> GetAllCaches()
        {
            return Caches.Values.ToImmutableList();
        }

        public CacheBase GetCache(string name)
        {
            return Caches.GetOrAdd(name, cacheName =>
            {
                CacheBase cache = CreateCacheImplementation(cacheName);

                IEnumerable<ICacheConfigurator> configurators =
                    Configuration.Configurators.Where(c => c.CacheName == null || c.CacheName == cacheName);

                foreach (ICacheConfigurator configurator in configurators)
                {
                    configurator.InitAction?.Invoke(cache);
                }

                return cache;
            });
        }

        public void Dispose()
        {
            Caches.Clear();
        }
        
        protected abstract CacheBase CreateCacheImplementation(string name);
    }
}