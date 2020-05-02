using System;
using System.Collections.Generic;

namespace Beleriand.Core.Configurations
{
    public interface ICachingConfiguration
    {
        IReadOnlyList<ICacheConfigurator> Configurators { get; }

        void ConfigureAll(Action<ICache> initAction);

        void Configure(string cacheName, Action<ICache> initAction);
    }
}