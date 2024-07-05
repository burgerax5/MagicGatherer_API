using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.DTOs;
using Newtonsoft.Json;
using System.Threading;

namespace MTG_Cards.Services
{
	public class Cache
	{
		private static readonly CancellationToken _cancellationToken = default;

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
	}
}
