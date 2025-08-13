using System.Security.Cryptography;
using System.Text.Json;

namespace HybridCacheDemo.Api.Utils
{
    public static class ETag
    {
        public static string ForObject<T>(T obj)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(json);
            return $"{Convert.ToHexString(hash)}";
        }
    }

}
