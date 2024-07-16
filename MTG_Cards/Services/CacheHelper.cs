using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Interfaces;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MTG_Cards.Services
{
	public class CacheHelper : ICacheHelper
	{
		
		private readonly IConnectionMultiplexer _redisConnection;
		private static readonly CancellationToken _cancellationToken = default;

		public CacheHelper(IConnectionMultiplexer redisConnection)
		{
			_redisConnection = redisConnection;
		}

		public static async Task<T?> GetCacheEntry<T>(IDistributedCache distributedCache, string key)
		{
			try
			{
				string? cachedValue = await distributedCache.GetStringAsync(key, _cancellationToken);

				if (string.IsNullOrEmpty(cachedValue))
				{
					return default;
				}

				return JsonConvert.DeserializeObject<T>(cachedValue);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error getting cache entry for key '{key}': {ex.Message}");
				return default;
			}
		}

		public static async Task SetCacheEntry<T>(IDistributedCache distributedCache, string key, T value, TimeSpan? expirationTime = null)
		{
			try
			{
				var cacheOptions = new DistributedCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = expirationTime ?? TimeSpan.FromMinutes(30)
				};

				string serializedValue = JsonConvert.SerializeObject(value);
				await distributedCache.SetStringAsync(key, serializedValue, cacheOptions, _cancellationToken);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error setting cache entry for key '{key}': {ex.Message}");
			}
		}

		public async Task ClearCacheEntries(string prefix)
		{
			var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
			var keys = server.Keys(pattern: $"{prefix}*").ToArray();
			var db = _redisConnection.GetDatabase();

			foreach (var key in keys)
			{
				await db.KeyDeleteAsync(key);
			}
		}
	}
}
