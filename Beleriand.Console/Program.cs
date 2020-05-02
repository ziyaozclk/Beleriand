using System.Collections.Generic;
using Beleriand.Core.Configurations;

namespace Beleriand.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new CachingConfiguration();

            MultiLevelCacheManager cacheManager = new MultiLevelCacheManager(configuration, "localhost");

            var applicationConfigurationCache = cacheManager.GetCache("ApplicationConfiguration");

            applicationConfigurationCache.Set("ApplicationConfiguration:1", new ApplicationConfiguration("Ziya", "1"));

            var result = applicationConfigurationCache.GetOrDefault<ApplicationConfiguration>(new List<string>
                {"ApplicationConfiguration:1", "ApplicationConfiguration:2"});

            System.Console.WriteLine();
        }
    }
}