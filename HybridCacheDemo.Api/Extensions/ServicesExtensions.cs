using HybridCacheDemo.Api.Repositories.Interface;
using HybridCacheDemo.Api.Worker;
using Microsoft.Extensions.Caching.Hybrid;

namespace HybridCacheDemo.Api.Extensions
{
    public static class ServicesExtensions
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add services to the container.
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Add MVC controllers
            services.AddControllers();

            // Redis (L2)
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration["Redis:Configuration"];
            });

            // HybridCache (L1+L2)
            services.AddHybridCache(options =>
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    LocalCacheExpiration = TimeSpan.FromMinutes(30), // L1 TTL
                    Expiration = TimeSpan.FromHours(24)           // L2 TTL
                };
            });

            // Repository
            services.AddSingleton<IItemRepository, InMemoryItemRepository>();
            services.AddHostedService<HotItemBackgroundWorker>();
        }
    }
}
