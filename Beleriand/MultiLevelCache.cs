using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beleriand.Core;
using Beleriand.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Beleriand
{
    public class MultiLevelCache : CacheBase
    {
        private const int HashSlotCount = 16384;

        private const string SyncChannelName = "NewtMultilevelCache_Sync";
        private const string ClearSyncChannelName = "NewtMultilevelCacheClear_Sync";

        private long[] _lastUpdated = new long[HashSlotCount];

        private readonly Dictionary<string, object> _caches;

        private readonly Guid _instanceId = Guid.NewGuid();

        private readonly IDatabase _redisDb;

        public MultiLevelCache(string name, ConnectionMultiplexer connectionMultiplexer) : base(name)
        {
            _caches = new Dictionary<string, object>();

            _redisDb = connectionMultiplexer.GetDatabase();

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
            key = GetLocalizedKey(key);

            var timestamp = Stopwatch.GetTimestamp() - 1;

            int keyHashSlot = -1;

            if (_caches.GetOrDefault(key) is LocalCacheEntry<T> inProcessCacheEntry)
            {
                keyHashSlot = inProcessCacheEntry.KeyHashSlot;

                lock (_lastUpdated)
                {
                    if (_lastUpdated[keyHashSlot] < inProcessCacheEntry.Timestamp)
                    {
                        return inProcessCacheEntry.Data;
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

                    if (value != null)
                    {
                        if (keyHashSlot == -1)
                        {
                            keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);
                        }

                        _caches.AddOrUpdate(key, new LocalCacheEntry<T>((ushort) keyHashSlot, timestamp, value));
                    }
                }
            }

            return value;
        }

        public override Dictionary<string, T> GetOrDefault<T>(List<string> keys)
        {
            keys = keys.Select(GetLocalizedKey).ToList();

            Dictionary<string, T> resultMap = new Dictionary<string, T>();

            Dictionary<string, int> keyHashSlotMap = new Dictionary<string, int>();

            var timestamp = Stopwatch.GetTimestamp() - 1;

            List<int> foundIndexList = new List<int>();

            var foundIndex = 0;
            foreach (var key in keys)
            {
                int keyHashSlot = -1;

                if (_caches.GetOrDefault(key) is LocalCacheEntry<T> inProcessCacheEntry)
                {
                    keyHashSlot = inProcessCacheEntry.KeyHashSlot;

                    lock (_lastUpdated)
                    {
                        if (_lastUpdated[keyHashSlot] < inProcessCacheEntry.Timestamp)
                        {
                            foundIndexList.Add(foundIndex);
                            resultMap.Add(key, inProcessCacheEntry.Data);
                        }
                    }
                }

                keyHashSlotMap.Add(key, keyHashSlot);

                foundIndex++;
            }

            if (foundIndexList.Count == keys.Count)
            {
                return resultMap;
            }

            var notFoundKeys = keys.Where((a, i) => !foundIndexList.Contains(i)).ToList();

            var luaScript =
                @"local values = redis.call('MGET', unpack(ARGV));
                  local results = {};
                  for i, key in ipairs(ARGV) do results[i] = values[i] end;
                  return results";

            var redisValues = (RedisValue[]) _redisDb.ScriptEvaluate(luaScript, null,
                keys.Select(a => new RedisValue(a)).ToArray());

            var values = redisValues.Where(s => !s.IsNull).Select(a =>
            {
                var serializedData = (string) a;

                if (serializedData.Length > 0)
                {
                    return JsonConvert.DeserializeObject<T>(serializedData);
                }

                return default;
            }).ToList();

            var notFoundIndex = 0;
            foreach (var value in values)
            {
                var key = notFoundKeys.ElementAt(notFoundIndex);

                int keyHashSlot = keyHashSlotMap.GetOrDefault(key);

                if (keyHashSlot == -1)
                {
                    keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);
                }

                resultMap.Add(key, value);
                _caches.AddOrUpdate(key, new LocalCacheEntry<T>((ushort) keyHashSlot, timestamp, value));
                notFoundIndex++;
            }

            return resultMap;
        }

        public override void Set<T>(string key, T value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
            if (value == null) throw new ArgumentNullException(nameof(value));

            key = GetLocalizedKey(key);

            var timestamp = Stopwatch.GetTimestamp() - 1;

            var keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);

            var publishValue = DataSyncMessage.Create(_instanceId, keyHashSlot).Serialize();

            string luaScript = @"
                    redis.call('SET', KEYS[1], ARGV[1])
                    redis.call('PUBLISH', ARGV[2], ARGV[3])
                  ";

            var serializeValue = JsonConvert.SerializeObject(value);

            _redisDb.ScriptEvaluate(luaScript, new RedisKey[] {key},
                new RedisValue[] {serializeValue, SyncChannelName, publishValue});
            _caches.AddOrUpdate(key, new LocalCacheEntry<T>(keyHashSlot, timestamp, value));
        }

        public override void Set<T>(Dictionary<string, T> values)
        {
            var itemCount = values.Count;

            if (itemCount == 0)
                throw new Exception("Values must not be null");

            var timestamp = Stopwatch.GetTimestamp() - 1;

            var keyValuePairString = values.Select((data, index) => $"KEYS[{index + 1}], ARGV[{index + 1}]");

            var parameters = string.Join(",", keyValuePairString);

            var multipleValueSetLuaScript = $"redis.call('MSET', {parameters})";

            RedisKey[] redisKeys = values.Keys.Select(a => new RedisKey(a)).ToArray();
            RedisValue[] redisValues =
                values.Values.Select(a => new RedisValue(JsonConvert.SerializeObject(a))).ToArray();

            _redisDb.ScriptEvaluate(multipleValueSetLuaScript, redisKeys, redisValues);

            var scriptArgs = new RedisValue[itemCount];

            string publishLuaScript = string.Empty;
            var insertIndex = 0;
            foreach (var keyValuePair in values)
            {
                var key = GetLocalizedKey(keyValuePair.Key);

                var keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);

                var publishValue = DataSyncMessage.Create(_instanceId, keyHashSlot).Serialize();

                scriptArgs[insertIndex] = publishValue;

                publishLuaScript += $"redis.call('PUBLISH', KEYS[1], ARGV[{insertIndex + 1}])";

                _caches.AddOrUpdate(key, new LocalCacheEntry<object>(keyHashSlot, timestamp, keyValuePair.Value));

                insertIndex++;
            }

            _redisDb.ScriptEvaluate(publishLuaScript, new RedisKey[] {SyncChannelName}, scriptArgs);
        }

        public override void Remove(string key)
        {
            key = GetLocalizedKey(key);

            var keyHashSlot = HashSlotCalculator.CalculateHashSlot(key);

            var publishValue = DataSyncMessage.Create(_instanceId, keyHashSlot).Serialize();

            string luaScript = @"
                    redis.call('PUBLISH', ARGV[1], ARGV[2])
                  ";

            _redisDb.ScriptEvaluate(luaScript, null, new RedisValue[] {SyncChannelName, publishValue});
            _caches.Remove(key);
        }

        public override void Clear()
        {
            var publishValue = DataSyncMessage.Create(_instanceId).Serialize();

            string luaScript = @"
                    redis.call('del', unpack(redis.call('keys', KEYS[1])))
                    redis.call('PUBLISH', ARGV[1], ARGV[2])
                  ";

            _redisDb.ScriptEvaluate(luaScript, new RedisKey[] {$"{Name}:*"},
                new RedisValue[] {ClearSyncChannelName, publishValue});
        }
    }
}