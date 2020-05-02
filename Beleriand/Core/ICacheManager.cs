using System;
using System.Collections.Generic;

namespace Beleriand.Core
{
    public interface ICacheManager : IDisposable
    {
        IReadOnlyList<CacheBase> GetAllCaches();

        CacheBase GetCache(string name);
    }
}