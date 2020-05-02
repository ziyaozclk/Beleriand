using System;

namespace Beleriand.Core.Configurations
{
    public interface ICacheConfigurator
    {
        string CacheName { get; }
        Action<ICache> InitAction { get; }
    }
}