namespace Beleriand.Core
{
    public class LocalCacheEntry<T>
    {
        public ushort KeyHashSlot { get; private set; }

        public long Timestamp { get; private set; }

        public T Data { get; private set; }

        public LocalCacheEntry(ushort keyHashSlot, long timestamp, T data)
        {
            KeyHashSlot = keyHashSlot;
            Timestamp = timestamp;
            Data = data;
        }
    }
}