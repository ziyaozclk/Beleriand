using Beleriand.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;

namespace Beleriand.Extensions
{
    public static class MultiLevelCacheRegistrationExtensions
    {
        public static void UseBeleriand(this IServiceCollection services, ICachingConfiguration configuration,
            string redisConnectionString)
        {
            services.AddSingleton(new MultiLevelCacheManager(configuration, redisConnectionString));
        }
    }
}