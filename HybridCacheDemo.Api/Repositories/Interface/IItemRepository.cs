using SecuringWebApi.Data.Entities;

namespace HybridCacheDemo.Api.Repositories.Interface
{
    public interface IItemRepository
    {
        Task<Item?> GetAsync(string id);
        Task UpsertAsync(Item item);
        Task<IReadOnlyList<string>> GetHotIdsAsync(int topN = 5);
        void ResetAccessCounts();
    }
}
