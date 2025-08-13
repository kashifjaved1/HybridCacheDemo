using HybridCacheDemo.Api.Repositories.Interface;
using Microsoft.Extensions.Caching.Hybrid;
using SecuringWebApi.Data.Entities;

namespace HybridCacheDemo.Api.Worker
{
    public sealed class HotItemBackgroundWorker : BackgroundService
    {
        private readonly HybridCache _cache;
        private readonly IItemRepository _repo;
        private readonly ILogger<HotItemBackgroundWorker> _logger;

        public HotItemBackgroundWorker(HybridCache cache, IItemRepository repo, ILogger<HotItemBackgroundWorker> logger)
        {
            _cache = cache;
            _repo = repo;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var hotIds = await _repo.GetHotIdsAsync(5);

                foreach (var id in hotIds)
                {
                    await _cache.GetOrCreateAsync<Item>(
                        $"item:{id}",
                        async _ =>
                        {
                            _logger.LogInformation("Refreshing hot item {Id} in cache", id);
                            return await _repo.GetAsync(id);
                        },
                        new HybridCacheEntryOptions
                        {
                            LocalCacheExpiration = TimeSpan.FromHours(6),
                            Expiration = TimeSpan.FromHours(24)
                        },
                        cancellationToken: cancellationToken
                    );
                }

                _repo.ResetAccessCounts();
                await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken);
            }
        }
    }


}
