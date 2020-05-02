using Beleriand.Core;
using Beleriand.Core.Configurations;
using StackExchange.Redis;

namespace Beleriand
{
    public class MultiLevelCacheManager : CacheManagerBase
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;

        public MultiLevelCacheManager(ICachingConfiguration configuration, string redisConnectionString)
            : base(configuration)
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        }

        protected override CacheBase CreateCacheImplementation(string name)
        {
            return new MultiLevelCache(name, _connectionMultiplexer);
        }
    }
}