using HybridCacheDemo.Api.Repositories.Interface;
using HybridCacheDemo.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Hybrid;
using SecuringWebApi.Data.Entities;

namespace HybridCacheDemo.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly HybridCache _cache;
        private readonly IItemRepository _repo;

        public ItemsController(HybridCache cache, IItemRepository repo)
        {
            _cache = cache;
            _repo = repo;
        }

        // READ: client should first check browser store using ETag,
        // then call this endpoint. Server path is: L1 -> L2 -> DB (HybridCache handles it).
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Get(string id, CancellationToken ct)
        {
            var item = await _cache.GetOrCreateAsync(
                key: $"item:{id}",
                factory: async token => await _repo.GetAsync(id), // DB when not in L1/L2
                //options: new HybridCacheEntryOptions
                //{
                //    ocalCacheExpiration = TimeSpan.FromHours(6),
                //    Expiration = TimeSpan.FromHours(24)
                //},
                tags: new[] { "items", $"item:{id}" },
                cancellationToken: ct);

            if (item is null) return NotFound();

            var etag = ETag.ForObject(item);
            if (Request.Headers.IfNoneMatch == etag)
                return StatusCode(StatusCodes.Status304NotModified);

            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, max-age=300"; // enables browser store step
            return Ok(item);
        }

        // WRITE: save to DB, then write-through to L1+L2 (HybridCache)
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Put(string id, [FromBody] Item payload, CancellationToken cancellationToken)
        {
            payload.Id = int.Parse(id);
            await _repo.UpsertAsync(payload);

            await _cache.SetAsync(
                key: $"item:{id}",
                value: payload,
                //options: new HybridCacheEntryOptions
                //{
                //    LocalCacheExpiration = TimeSpan.FromHours(6),
                //    Expiration = TimeSpan.FromHours(24)
                //},
                tags: new[] { "items", $"item:{id}" },
                cancellationToken: cancellationToken);

            return Ok(new { updated = id });
        }

        // EVICTION (by key): clears L1+L2 copies
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKey(string id, CancellationToken ct)
        {
            await _cache.RemoveAsync($"item:{id}", ct);
            return Ok(new { removed = id });
        }

        // EVICTION (by tag): clear a group, e.g., "items"
        [HttpDelete("tag/{tag}")]
        public async Task<IActionResult> DeleteByTag(string tag, CancellationToken ct)
        {
            await _cache.RemoveByTagAsync(tag, ct);
            return Ok(new { removedTag = tag });
        }

        // REFRESH (explicit): reload from DB and re-cache into L1+L2
        [HttpPost("{id}/refresh")]
        public async Task<IActionResult> Refresh(string id, CancellationToken ct)
        {
            var fresh = await _repo.GetAsync(id);
            if (fresh is null) return NotFound();

            await _cache.SetAsync(
                $"item:{id}",
                fresh,
                //new HybridCacheEntryOptions
                //{
                //    LocalCacheExpiration = TimeSpan.FromHours(6),
                //    Expiration = TimeSpan.FromHours(24)
                //},
                tags: new[] { "items", $"item:{id}" },
                cancellationToken: ct);

            return Ok(new { refreshed = id });
        }

        /// <summary>
        /// Get item by ID.
        /// </summary>
        /// <param name="id">Item ID</param>
        /// <param name="ifNoneMatch">ETag to check for conditional GET</param>
        /// <returns></returns>
        /// <response code="200">Returns the item</response>
        /// <response code="304">Not Modified</response>
        [HttpGet("getItemForTestingPurposesOnly/{id}")]
        [ProducesResponseType(typeof(Item), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status304NotModified)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        public async Task<IActionResult> GetItemForSwaggerOnly(string id, [FromHeader(Name = "If-None-Match")] string ifNoneMatch, CancellationToken cancellationToken)
        {
            var item = await _cache.GetOrCreateAsync(
                key: $"item:{id}",
                factory: async token => await _repo.GetAsync(id), // DB when not in L1/L2
                //options: new HybridCacheEntryOptions
                //{
                //    ocalCacheExpiration = TimeSpan.FromHours(6),
                //    Expiration = TimeSpan.FromHours(24)
                //},
                tags: new[] { "items", $"item:{id}" },
                cancellationToken: cancellationToken);

            if (item is null) return NotFound();

            var etag = ETag.ForObject(item);
            if (ifNoneMatch == etag)
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = etag;
            Response.Headers.CacheControl = "private, max-age=300"; // enables browser store step
            return Ok(item);
        }
    }
}
