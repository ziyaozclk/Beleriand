using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beleriand.Core;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Beleriand
{
    public class MultiLevelCache : CacheBase
    {
        private const int HashSlotCount = 16384;

        private const string SyncChannelName = "NewtMultilevelCache_Sync";
        private const string ClearSyncChannelName = "NewtMultilevelCacheClear_Sync";

        private readonly Guid _instanceId = Guid.NewGuid();

        private long[] _lastUpdated = new long[HashSlotCount];

        public readonly Dictionary<string, LocalCacheEntry<object>> _caches;

        private readonly IDatabase _redisDb;

        public MultiLevelCache(string name, ConnectionMultiplexer connectionMultiplexer) : base(name)
        {
            _redisDb = connectionMultiplexer.GetDatabase();

            _caches = new Dictionary<string, LocalCacheEntry<object>>();

            connectionMultiplexer.GetSubscriber()
                .Subscribe(SyncChannelName, DataSynchronizationMessageHandler);
            connectionMultiplexer.GetSubscriber()
                .Subscribe(ClearSyncChannelName, DataClearSynchronizationMessageHandler);
        }

        private void DataSynchronizationMessageHandler(RedisChannel channel, RedisValue message)
        {
            if (string.Compare(channel, SyncChannelName, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                return;
            }

            var dataSyncMessage = DataSyncMessage.Deserialize(message);

            if (dataSyncMessage.SenderInstanceId.Equals(_instanceId))
            {
                return;
            }

            lock (_lastUpdated)
            {
                _lastUpdated[dataSyncMessage.KeyHashSlot] = Stopwatch.GetTimestamp();
            }
        }

        private void DataClearSynchronizationMessageHandler(RedisChannel channel, RedisValue message)
        {
            if (string.Compare(channel, ClearSyncChannelName, StringComparison.InvariantCultureIgnoreCase) != 0)
            {
                return;
            }

            var dataSyncMessage = DataSyncMessage.Deserialize(message);

            if (!dataSyncMessage.SenderInstanceId.Equals(_instanceId))
            {
                return;
            }

            _caches.Clear();

            lock (_lastUpdated)
            {
                _lastUpdated = new long[HashSlotCount];
            }
        }

        public override T GetOrDefault<T>(string key)
        {
            var timestamp = Stopwatch.GetTimestamp() - 1;

            int keyHashSlot = -1;

            if (_caches.TryGetValue(key, out LocalCacheEntry<object> inMemoryValue))
            {
                if (inMemoryValue is LocalCacheEntry<T> inProcessCacheEntry)
                {
                    keyHashSlot = inProcessCacheEntry.KeyHashSlot;

                    lock (_lastUpdated)
                    {
                        if (_lastUpdated[keyHashSlot] < inMemoryValue.Timestamp)
                        {
                            return inProcessCacheEntry.Data;
                        }
                    }
                }
            }

            T value = default;

            string luaScript = @"
                local result={}
                result[1] = redis.call('GET', KEYS[1])
                return result;
              ";

            var results = (RedisValue[]) _redisDb.ScriptEvaluate(luaScript, new RedisKey[] {key});

            if (!results[0].IsNull)
            {
                var serializedData = (string) results[0];

                if (serializedData.Length > 0)
                {
                    value = JsonConvert.DeserializeObject<T>(serializedData);

                    if (keyHashSlot == -1)
                    {
                        keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);
                    }

                    _caches.Add(key, new LocalCacheEntry<object>((ushort) keyHashSlot, timestamp, value));
                }
            }

            return value;
        }

        public override Dictionary<string, T> GetOrDefault<T>(List<string> keys)
        {
            Dictionary<string, T> returnMap = new Dictionary<string, T>();

            var timestamp = Stopwatch.GetTimestamp() - 1;

            int keyHashSlot = -1;

            List<int> foundIndexList = new List<int>();

            int index = 0;
            foreach (var key in keys)
            {
                if (_caches.TryGetValue(key, out LocalCacheEntry<object> inMemoryValue))
                {
                    if (inMemoryValue is LocalCacheEntry<T> inProcessCacheEntry)
                    {
                        keyHashSlot = inProcessCacheEntry.KeyHashSlot;

                        lock (_lastUpdated)
                        {
                            if (_lastUpdated[keyHashSlot] < inMemoryValue.Timestamp)
                            {
                                foundIndexList.Add(index);
                                returnMap.Add(key, inProcessCacheEntry.Data);
                            }
                        }
                    }
                }

                index++;
            }

            if (foundIndexList.Count == keys.Count)
            {
                return returnMap;
            }

            var notFoundKeys = keys.Where((a, i) => !foundIndexList.Contains(i)).ToList();

            var luaScript =
                @"local values = redis.call('MGET', unpack(ARGV));
                  local results = {};
                  for i, key in ipairs(ARGV) do results[i] = values[i] end;
                  return results";

            var results =
                (RedisValue[]) _redisDb.ScriptEvaluate(luaScript, null,
                    notFoundKeys.Select(a => new RedisValue(a)).ToArray());

            var newIndex = 0;
            foreach (var redisValue in results)
            {
                var serializedData = (string) redisValue;

                if (serializedData.Length > 0)
                {
                    var key = notFoundKeys.ElementAt(newIndex);
                    var value = JsonConvert.DeserializeObject<T>(serializedData);

                    returnMap.Add(key, value);

                    if (keyHashSlot == -1)
                    {
                        keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);
                    }

                    _caches.Add(key, new LocalCacheEntry<object>((ushort) keyHashSlot, timestamp, value));
                }

                newIndex++;
            }

            return returnMap;
        }

        public override void Set<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var timestamp = Stopwatch.GetTimestamp() - 1;

            var keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);

            string serializedData = JsonConvert.SerializeObject(value);

            string luaScript = @"
                    redis.call('SET', KEYS[1], ARGV[1])
                    redis.call('PUBLISH', ARGV[2], ARGV[3])
                  ";

            var scriptArgs = new RedisValue[3];
            scriptArgs[0] = serializedData;
            scriptArgs[1] = SyncChannelName;
            scriptArgs[2] = DataSyncMessage.Create(_instanceId, keyHashSlot).Serialize();

            _redisDb.ScriptEvaluate(luaScript, new RedisKey[] {key}, scriptArgs);
            _caches.Add(key, new LocalCacheEntry<object>(keyHashSlot, timestamp, value));
        }

        public override void Set<T>(Dictionary<string, T> values)
        {
            throw new System.NotImplementedException();
        }

        public override void Remove(string key)
        {
            var keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);

            var scriptArgs = new RedisValue[2];
            scriptArgs[1] = SyncChannelName;
            scriptArgs[2] = DataSyncMessage.Create(_instanceId, keyHashSlot).Serialize();

            string luaScript = @"
                    redis.call('PUBLISH', ARGV[1], ARGV[2])
                  ";

            _redisDb.ScriptEvaluate(luaScript, new RedisKey[] {""}, scriptArgs);

            _caches.Remove(key);
        }

        public override void Clear()
        {
            throw new System.NotImplementedException();
        }
    }
}