using System.Collections.Concurrent;
using SecuringWebApi.Data.Entities;

namespace HybridCacheDemo.Api.Repositories.Interface
{
    public sealed class InMemoryItemRepository : IItemRepository
    {
        private readonly ConcurrentDictionary<string, Item> _db = new();
        private readonly ConcurrentDictionary<string, int> _accessCounts = new();

        public Task<Item?> GetAsync(string id)
        {
            _db.TryGetValue(id, out var item);
            if (item != null)
            {
                // Track access count
                _accessCounts.AddOrUpdate(id, 1, (_, count) => count + 1);
            }
            return Task.FromResult(item);
        }

        public Task UpsertAsync(Item item)
        {
            item.UpdatedAt = DateTimeOffset.UtcNow;
            _db[item.Id.ToString()] = item;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the top N most accessed item IDs in the last period.
        /// </summary>
        public Task<IReadOnlyList<string>> GetHotIdsAsync(int topN = 5)
        {
            var hotIds = _accessCounts
                .OrderByDescending(kvp => kvp.Value)
                .Take(topN)
                .Select(kvp => kvp.Key)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(hotIds);
        }

        /// <summary>
        /// Optional: clear stats periodically if you want "fresh" hot data.
        /// </summary>
        public void ResetAccessCounts() => _accessCounts.Clear();
    }
}
