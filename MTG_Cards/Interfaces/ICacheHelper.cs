using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System.Threading;

namespace MTG_Cards.Interfaces
{
	public interface ICacheHelper
	{
		static Task<T?> GetCacheEntry<T>(IDistributedCache distributedCache, string key) => throw new NotImplementedException();
		static Task SetCacheEntry<T>(IDistributedCache distributedCache, string key, T value, TimeSpan? expirationTime = null) => throw new NotImplementedException();
		Task ClearCacheEntries(string prefix);
	}
}
