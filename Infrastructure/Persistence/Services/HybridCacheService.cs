using Application.Common.Helper;
using Application.Dto.ProductDTOs;
using Application.Interfaces.Services;
using Infrastructure.Persistence.Configurations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Services
{
    public class HybridCacheService : IHybridCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly IDatabase _redisDatabase;
        private readonly IConnectionMultiplexer _redis;
        private readonly HybridCacheOptions _options;
        private readonly ILogger<HybridCacheService> _logger;

        //  PERFORMANCE METRICS
        private readonly ConcurrentDictionary<string, CacheMetrics> _metrics = new();
        private readonly Timer _metricsTimer;

        public HybridCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IConnectionMultiplexer redis,
            IOptions<HybridCacheOptions> options,
            ILogger<HybridCacheService> logger)
        {
            _memoryCache = memoryCache;
            _distributedCache = distributedCache;
            _redis = redis;
            _redisDatabase = redis.GetDatabase();
            _options = options.Value;
            _logger = logger;

            //  SETUP METRICS LOGGING
            _metricsTimer = new Timer(LogMetrics, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("🚀 HybridCacheService initialized with Redis Cloud connection");
        }

        //  CORE GET METHOD - L1 → L2 → MISS Strategy
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheType = typeof(T).Name.ToLower();

            try
            {
                // 🏃‍♂️ L1: CHECK MEMORY CACHE FIRST
                if (_memoryCache.TryGetValue(key, out T? memoryValue))
                {
                    stopwatch.Stop();
                    RecordCacheHit(cacheType, "L1", stopwatch.ElapsedMilliseconds);
                    _logger.LogDebug("🎯 L1 HIT: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                    return memoryValue;
                }

                // ⚡ L2: CHECK REDIS CACHE
                var redisValue = await _distributedCache.GetStringAsync(key, cancellationToken);
                if (!string.IsNullOrEmpty(redisValue))
                {
                    var deserializedValue = JsonConvert.DeserializeObject<T>(redisValue, _options.JsonSettings);

                    //  BACKFILL L1 CACHE
                    var memoryCacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = _options.L1CacheExpiry,
                        Priority = CacheItemPriority.High,
                        Size = EstimateSize(deserializedValue)
                    };
                    _memoryCache.Set(key, deserializedValue, memoryCacheOptions);

                    stopwatch.Stop();
                    RecordCacheHit(cacheType, "L2", stopwatch.ElapsedMilliseconds);
                    _logger.LogDebug("⚡ L2 HIT: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                    return deserializedValue;
                }

                //  CACHE MISS
                stopwatch.Stop();
                RecordCacheMiss(cacheType, stopwatch.ElapsedMilliseconds);
                _logger.LogDebug(" CACHE MISS: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
                return default(T);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, " Cache GET error for key {Key}: {Error}", key, ex.Message);
                RecordCacheError(cacheType, stopwatch.ElapsedMilliseconds);
                return default(T);
            }
        }

        //  NEW: BULK GET - Single network call for multiple keys
        public async Task<Dictionary<string, T?>> GetBulkAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var keysList = keys.ToList();
            if (!keysList.Any()) return new Dictionary<string, T?>();

            var stopwatch = Stopwatch.StartNew();
            var cacheType = typeof(T).Name.ToLower();
            var results = new Dictionary<string, T?>();

            try
            {
                _logger.LogInformation("CACHE REQUEST: Type={Type}, Keys=[{keys}]", cacheType, string.Join(",", keysList));

                //  STEP 1: Check L1 cache (memory) for all keys
                var l1Hits = new List<string>();
                var l1Misses = new List<string>();

                foreach (var key in keysList)
                {
                    if (_memoryCache.TryGetValue(key, out T? memoryValue))
                    {
                        results[key] = memoryValue;
                        l1Hits.Add(key);
                        _logger.LogDebug("L1 HIT: {Key}", key);
                    }
                    else
                    {
                        l1Misses.Add(key);
                        _logger.LogDebug("L1 Miss : {key}", key);
                    }
                }

                _logger.LogInformation("🎯 L1 SUMMARY: {Hits}/{Total} hits, Missed=[{Misses}]", 
                l1Hits.Count, keysList.Count, string.Join(", ", l1Misses));

                //  STEP 2: Bulk fetch from Redis for L1 misses
                if (l1Misses.Any())
                {
                     _logger.LogInformation(" REDIS LOOKUP: Checking {Count} keys: [{Keys}]", 
                    l1Misses.Count, string.Join(", ", l1Misses));

                    // Use Redis MGET for bulk retrieval
                    var redisStopWatch = Stopwatch.StartNew();
                    var redisKeys = l1Misses.Select(k => (RedisKey)k).ToArray();
                    var redisValues = await _redisDatabase.StringGetAsync(redisKeys);
                    _logger.LogInformation("⚡ REDIS RESPONSE: {ElapsedMs}ms for {Count} keys", 
                    redisStopWatch.ElapsedMilliseconds, redisKeys.Length);

                    var l2Hits = new List<string>();
                    var backfillTasks = new List<Task>();

                    for (int i = 0; i < l1Misses.Count; i++)
                    {
                        var key = l1Misses[i];
                        var redisValue = redisValues[i];

                        if (redisValue.HasValue)
                        {
                            _logger.LogDebug("⚡ REDIS HIT: {Key} = {Length} chars", key, redisValue.ToString().Length);
                            try
                            {
                                var deserializedValue = JsonConvert.DeserializeObject<T>(redisValue!, _options.JsonSettings);
                                results[key] = deserializedValue;
                                l2Hits.Add(key);

                                //  Backfill L1 cache asynchronously
                                var memoryCacheOptions = new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = _options.L1CacheExpiry,
                                    Priority = CacheItemPriority.High,
                                    Size = EstimateSize(deserializedValue)
                                };

                                backfillTasks.Add(Task.Run(() =>
                                {
                                    _memoryCache.Set(key, deserializedValue, memoryCacheOptions);
                                    _logger.LogDebug("L1 BACKFILL : {key}", key);
                            }));
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, " DESERIALIZATION ERROR: {Key} - {Error}", key, ex.Message);
                                results[key] = default(T);
                            }
                        }
                        else
                        {
                            _logger.LogDebug(" REDIS MISS: {Key}", key);
                            results[key] = default(T);
                        }
                    }

                    // Wait for backfill operations
                    await Task.WhenAll(backfillTasks);

                    _logger.LogInformation("⚡ REDIS SUMMARY: {Hits}/{Misses} hits, Found=[{HitKeys}]", 
                    l2Hits.Count, l1Misses.Count, string.Join(", ", l2Hits));
                }

                stopwatch.Stop();
                
                var totalHits = l1Hits.Count + (l1Misses.Count - (keysList.Count - results.Values.Count(v => v != null)));
                var hitRate = (double)totalHits / keysList.Count * 100;

                RecordBulkCacheOperation(cacheType, keysList.Count, totalHits, stopwatch.ElapsedMilliseconds);
                
                _logger.LogInformation(" CACHE RESULT: {Keys} keys, {HitRate:F1}% hit rate in {ElapsedMs}ms", 
                keysList.Count, hitRate, stopwatch.ElapsedMilliseconds);

                return results;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, " CACHE ERROR: {KeyCount} keys failed - {Error}", keysList.Count, ex.Message);

                // Return empty results for all keys on error
                return keysList.ToDictionary(k => k, k => default(T));
            }
        }

        //  CORE SET METHOD - Write-Through Strategy
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var cacheType = typeof(T).Name.ToLower();

            try
            {
                var effectiveExpiry = expiry ?? _options.DefaultExpiry;
                var serializedValue = JsonConvert.SerializeObject(value, _options.JsonSettings);

                // 🏃‍♂️ L1: SET MEMORY CACHE
                var l1Expiry = TimeSpan.FromMinutes(Math.Min(effectiveExpiry.TotalMinutes, _options.L1CacheExpiry.TotalMinutes));
                var memoryCacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = l1Expiry,
                    Priority = CacheItemPriority.High,
                    Size = EstimateSize(value)
                };
                _memoryCache.Set(key, value, memoryCacheOptions);

                // ⚡ L2: SET REDIS CACHE
                var distributedCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveExpiry
                };
                await _distributedCache.SetStringAsync(key, serializedValue, distributedCacheOptions, cancellationToken);

                stopwatch.Stop();
                RecordCacheWrite(cacheType, stopwatch.ElapsedMilliseconds);
                _logger.LogDebug(" CACHE SET: {Key} (L1+L2) in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, " Cache SET error for key {Key}: {Error}", key, ex.Message);
                RecordCacheError(cacheType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        //  NEW: BULK SET - Single network call for multiple key-value pairs
        public async Task SetBulkAsync<T>(Dictionary<string, T> keyValuePairs, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
        {
            if (!keyValuePairs.Any()) return;

            var stopwatch = Stopwatch.StartNew();
            var cacheType = typeof(T).Name.ToLower();

            try
            {
                var effectiveExpiry = expiry ?? _options.DefaultExpiry;
                var l1Expiry = TimeSpan.FromMinutes(Math.Min(effectiveExpiry.TotalMinutes, _options.L1CacheExpiry.TotalMinutes));

                //  STEP 1: Prepare data for Redis pipeline
                var redisBatch = _redisDatabase.CreateBatch();
                var redisTasks = new List<Task>();

                foreach (var kvp in keyValuePairs)
                {
                    try
                    {
                        var serializedValue = JsonConvert.SerializeObject(kvp.Value, _options.JsonSettings);
                        
                        //  L1: Set in memory cache immediately
                        var memoryCacheOptions = new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = l1Expiry,
                            Priority = CacheItemPriority.High,
                            Size = EstimateSize(kvp.Value)
                        };
                        _memoryCache.Set(kvp.Key, kvp.Value, memoryCacheOptions);

                        //  L2: Add to Redis batch
                        redisTasks.Add(redisBatch.StringSetAsync(kvp.Key, serializedValue, effectiveExpiry));
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to serialize value for key {Key}", kvp.Key);
                    }
                }

                //  STEP 2: Execute Redis batch operation
                redisBatch.Execute();
                await Task.WhenAll(redisTasks);

                stopwatch.Stop();
                RecordBulkCacheWrite(cacheType, keyValuePairs.Count, stopwatch.ElapsedMilliseconds);
                
                _logger.LogInformation(" BULK SET: {Keys} keys (L1+L2) in {ElapsedMs}ms", 
                    keyValuePairs.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, " Bulk cache SET error for {KeyCount} keys: {Error}", keyValuePairs.Count, ex.Message);
                RecordCacheError(cacheType, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        //  CONVENIENCE METHOD - Set with cache type
        public async Task SetAsync<T>(string key, T value, string cacheType, CancellationToken cancellationToken = default)
        {
            var expiry = _options.GetExpiry(cacheType);
            await SetAsync(key, value, expiry, cancellationToken);
        }

        public async Task SetBulkAsync<T>(Dictionary<string, T> keyValuePairs, string cacheType, CancellationToken cancellationToken = default)
        {
            var expiry = _options.GetExpiry(cacheType);
            await SetBulkAsync(keyValuePairs, expiry, cancellationToken);
        }

        //  OPTIMIZED: E-commerce specific bulk methods
        public async Task<Dictionary<int, ProductDTO?>> GetProductsBulkAsync(List<int> productIds, CancellationToken cancellationToken = default)
        {
            if (!productIds?.Any() == true) return new Dictionary<int, ProductDTO?>();

            var keys = productIds!.Select(id => $"product:{id}").ToList();
            var cacheResults = await GetBulkAsync<ProductDTO>(keys, cancellationToken);

            // Convert back to product ID dictionary
            var results = new Dictionary<int, ProductDTO?>();
            for (int i = 0; i < productIds!.Count; i++)
            {
                var productId = productIds[i];
                var key = keys[i];
                results[productId] = cacheResults.TryGetValue(key, out var product) ? product : null;
            }

            _logger.LogDebug("🛍️ PRODUCTS BULK: Retrieved {CachedCount} of {RequestedCount} products",
                results.Values.Count(v => v != null), productIds!.Count);

            return results;
        }

        public async Task SetProductsBulkAsync(Dictionary<int, ProductDTO> products, CancellationToken cancellationToken = default)
        {
            if (!products?.Any() == true) return;

            var keyValuePairs = products!.ToDictionary(
                kvp => $"product:{kvp.Key}",
                kvp => kvp.Value
            );

            await SetBulkAsync(keyValuePairs, "products", cancellationToken);
            _logger.LogDebug("🛍️ PRODUCTS BULK: Cached {Count} products", products!.Count);
        }

        public async Task<Dictionary<int, ProductPriceInfoDTO?>> GetPricingBulkAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default)
        {
            if (!productIds?.Any() == true) return new Dictionary<int, ProductPriceInfoDTO?>();

            //  USER-SPECIFIC vs GLOBAL PRICING
            var keyPrefix = userId.HasValue ? $"pricing:user:{userId}:product:" : "pricing:global:product:";
            var keys = productIds!.Select(id => $"{keyPrefix}{id}").ToList();
            
            var cacheResults = await GetBulkAsync<ProductPriceInfoDTO>(keys, cancellationToken);

            // Convert back to product ID dictionary
            var results = new Dictionary<int, ProductPriceInfoDTO?>();
            for (int i = 0; i < productIds!.Count; i++)
            {
                var productId = productIds[i];
                var key = keys[i];
                results[productId] = cacheResults.TryGetValue(key, out var pricing) ? pricing : null;
            }

            _logger.LogDebug("💰 PRICING BULK: Retrieved {CachedCount} of {RequestedCount} pricing records",
                results.Values.Count(v => v != null), productIds!.Count);

            return results;
        }

        public async Task SetPricingBulkAsync(Dictionary<int, ProductPriceInfoDTO> pricing, int? userId = null, CancellationToken cancellationToken = default)
        {
            if (!pricing?.Any() == true) return;

            var keyPrefix = userId.HasValue ? $"pricing:user:{userId}:product:" : "pricing:global:product:";
            var keyValuePairs = pricing!.ToDictionary(
                kvp => $"{keyPrefix}{kvp.Key}",
                kvp => kvp.Value
            );

            await SetBulkAsync(keyValuePairs, "pricing", cancellationToken);
            _logger.LogDebug("💰 PRICING BULK: Cached {Count} pricing records", pricing!.Count);
        }

        //  LEGACY METHODS (for backward compatibility) - Now use bulk operations internally
        public async Task<List<ProductDTO>> GetProductsAsync(List<int> productIds, CancellationToken cancellationToken = default)
        {
            var bulkResults = await GetProductsBulkAsync(productIds, cancellationToken);
            return bulkResults.Values.Where(p => p != null).ToList()!;
        }

        public async Task SetProductsAsync(List<ProductDTO> products, CancellationToken cancellationToken = default)
        {
            if (!products?.Any() == true) return;
            
            var dictionary = products!.ToDictionary(p => p.Id, p => p);
            await SetProductsBulkAsync(dictionary, cancellationToken);
        }

        public async Task<List<ProductPriceInfoDTO>> GetPricingAsync(List<int> productIds, int? userId = null, CancellationToken cancellationToken = default)
        {
            var bulkResults = await GetPricingBulkAsync(productIds, userId, cancellationToken);
            return bulkResults.Values.Where(p => p != null).ToList()!;
        }

        public async Task SetPricingAsync(List<ProductPriceInfoDTO> pricing, int? userId = null, CancellationToken cancellationToken = default)
        {
            if (!pricing?.Any() == true) return;
            
            var dictionary = pricing!.ToDictionary(p => p.ProductId, p => p);
            await SetPricingBulkAsync(dictionary, userId, cancellationToken);
        }

        //  BULK REMOVE
        public async Task RemoveBulkAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            var keysList = keys.ToList();
            if (!keysList.Any()) return;

            try
            {
                // Remove from L1 cache
                foreach (var key in keysList)
                {
                    _memoryCache.Remove(key);
                }

                // Remove from L2 cache using Redis pipeline
                var redisKeys = keysList.Select(k => (RedisKey)k).ToArray();
                await _redisDatabase.KeyDeleteAsync(redisKeys);

                _logger.LogDebug(" BULK REMOVE: {Count} keys", keysList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Bulk cache REMOVE error for {KeyCount} keys: {Error}", keysList.Count, ex.Message);
            }
        }

        //  CACHE INVALIDATION
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            await RemoveBulkAsync(new[] { key }, cancellationToken);
        }

        public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug(" Searching for keys matching pattern: {Pattern}", pattern);
                
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                
                //  IMPROVED PATTERN MATCHING - Handle wildcards better
                var searchPattern = pattern.Contains("*") ? pattern : $"*{pattern}*";
                var keys = server.Keys(pattern: searchPattern, pageSize: 1000).ToArray();

                if (keys.Any())
                {
                    _logger.LogInformation(" Found {Count} keys matching pattern '{Pattern}': [{Keys}]", 
                        keys.Length, pattern, string.Join(", ", keys.Take(5).Select(k => k.ToString())));

                    // Remove from L1 cache (memory)
                    foreach (var key in keys)
                    {
                        _memoryCache.Remove(key.ToString());
                    }

                    // Remove from L2 cache (Redis) in batches
                    const int batchSize = 100;
                    for (int i = 0; i < keys.Length; i += batchSize)
                    {
                        var batch = keys.Skip(i).Take(batchSize).ToArray();
                        await _redisDatabase.KeyDeleteAsync(batch);
                        
                        _logger.LogDebug(" Removed batch {BatchNum}: {Count} keys", (i / batchSize) + 1, batch.Length);
                    }

                    _logger.LogInformation(" Successfully removed {Count} keys matching pattern '{Pattern}'", keys.Length, pattern);
                }
                else
                {
                    _logger.LogDebug("📭 No keys found matching pattern: {Pattern}", pattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Failed to remove keys by pattern '{Pattern}': {Error}", pattern, ex.Message);
                throw;
            }
        }

        public async Task InvalidateProductsAsync(List<int> productIds, CancellationToken cancellationToken = default)
        {
            var keysToRemove = new List<string>();
            
            foreach (var id in productIds)
            {
                keysToRemove.Add($"product:{id}");
            }

            // Also remove pricing keys
            await RemoveByPatternAsync($"pricing:*:product:*", cancellationToken);
            await RemoveBulkAsync(keysToRemove, cancellationToken);
            
            _logger.LogInformation(" PRODUCTS INVALIDATED: {Count} products", productIds.Count);
        }

        

        //  CACHE HEALTH MONITORING
        public async Task<CacheHealthInfo> GetHealthAsync(CancellationToken cancellationToken = default)
        {
            var health = new CacheHealthInfo();

            try
            {
                // Test Redis connection
                var stopwatch = Stopwatch.StartNew();
                await _redisDatabase.PingAsync();
                stopwatch.Stop();

                health.IsRedisConnected = true;
                health.RedisLatency = stopwatch.Elapsed;

                // Get Redis memory usage
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                var info = await server.InfoAsync("memory");
                var memoryUsage = info.FirstOrDefault(x => x.Key == "used_memory")?.FirstOrDefault();
                if (memoryUsage.HasValue && long.TryParse(memoryUsage.Value.Value, out var memory))
                {
                    health.RedisMemoryUsage = memory;
                }

                // Test connection with a simple operation
                await _redisDatabase.StringSetAsync("health:check", DateTime.UtcNow.ToString(), TimeSpan.FromMinutes(1));
                var testValue = await _redisDatabase.StringGetAsync("health:check");
                health.IsRedisConnected = testValue.HasValue;
            }
            catch (Exception ex)
            {
                health.IsRedisConnected = false;
                health.Warnings.Add($"Redis connection error: {ex.Message}");
                _logger.LogWarning(" Redis health check failed: {Error}", ex.Message);
            }

            // Memory cache health
            health.IsMemoryCacheHealthy = _memoryCache != null;

            // Cache hit rates
            health.CacheHitRates = _metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => (int)((double)kvp.Value.Hits / Math.Max(kvp.Value.Total, 1) * 100)
            );

            return health;
        }

        //  CACHE WARM-UP for critical data
        public async Task WarmUpAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(" Starting cache warm-up...");

            try
            {
                // Warm up critical data (implement based on your needs)
                // This is a placeholder - you'd implement based on your most accessed data

                _logger.LogInformation(" Cache warm-up completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Cache warm-up failed: {Error}", ex.Message);
            }
        }

        //  HELPER METHODS
        private static long EstimateSize<T>(T? value)
        {
            if (value == null) return 1;
            try
            {
                var json = JsonConvert.SerializeObject(value);
                var sizeInBytes = System.Text.Encoding.UTF8.GetByteCount(json);

                // Convert to cache units (typically 1 unit = 1 byte, but we'll use a reasonalbe multiplier)
                return Math.Max(1, sizeInBytes / 1024); // Return size in KB
            }
            catch
            {
                // Fallback estimation based on type
                return value switch
                {
                    string s => Math.Max(1, s.Length / 1024),
                    System.Collections.ICollection collection => Math.Max(1, collection.Count),
                    _ => 1 // Default size for unknown types
                };
            }
        }

        private void RecordCacheHit(string type, string level, long elapsedMs)
        {
            _metrics.AddOrUpdate(type,
                new CacheMetrics { Hits = 1, Total = 1, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Hits = existing.Hits + 1,
                    Total = existing.Total + 1,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void RecordCacheMiss(string type, long elapsedMs)
        {
            _metrics.AddOrUpdate(type,
                new CacheMetrics { Misses = 1, Total = 1, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Hits = existing.Hits,
                    Misses = existing.Misses + 1,
                    Total = existing.Total + 1,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void RecordCacheWrite(string type, long elapsedMs)
        {
            _metrics.AddOrUpdate(type,
                new CacheMetrics { Writes = 1, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Hits = existing.Hits,
                    Misses = existing.Misses,
                    Writes = existing.Writes + 1,
                    Total = existing.Total,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void RecordBulkCacheOperation(string type, int totalKeys, int hits, long elapsedMs)
        {
            _metrics.AddOrUpdate($"{type}_bulk",
                new CacheMetrics { Hits = hits, Total = totalKeys, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Hits = existing.Hits + hits,
                    Total = existing.Total + totalKeys,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void RecordBulkCacheWrite(string type, int keyCount, long elapsedMs)
        {
            _metrics.AddOrUpdate($"{type}_bulk_write",
                new CacheMetrics { Writes = keyCount, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Writes = existing.Writes + keyCount,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void RecordCacheError(string type, long elapsedMs)
        {
            _metrics.AddOrUpdate(type,
                new CacheMetrics { Errors = 1, TotalLatency = elapsedMs },
                (key, existing) => new CacheMetrics
                {
                    Hits = existing.Hits,
                    Misses = existing.Misses,
                    Writes = existing.Writes,
                    Errors = existing.Errors + 1,
                    Total = existing.Total,
                    TotalLatency = existing.TotalLatency + elapsedMs
                });
        }

        private void LogMetrics(object? state)
        {
            if (!_metrics.Any()) return;

            var summary = _metrics.Select(kvp => new
            {
                Type = kvp.Key,
                HitRate = kvp.Value.Total > 0 ? (double)kvp.Value.Hits / kvp.Value.Total * 100 : 0,
                AvgLatency = kvp.Value.Total > 0 ? (double)kvp.Value.TotalLatency / kvp.Value.Total : 0,
                Total = kvp.Value.Total,
                Errors = kvp.Value.Errors
            }).ToList();

            _logger.LogInformation(" CACHE METRICS: {MetricsSummary}",
                JsonConvert.SerializeObject(summary, Formatting.Indented));
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
        }
    }
}