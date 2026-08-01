using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Infrastructure.Redis;

public sealed class RedisLiveLocationCache(
    IConnectionMultiplexer multiplexer,
    IOptions<RedisSettings> options,
    ILogger<RedisLiveLocationCache> logger) : ILiveLocationCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisSettings _settings = options.Value;

    public async Task SaveAsync(LiveLocationUpdateDto update, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var db = multiplexer.GetDatabase();
        var memberKey = MemberKey(update.TripId, update.UserId);
        var tripKey = TripKey(update.TripId);
        var payload = JsonSerializer.Serialize(update, JsonOptions);
        var ttl = TimeSpan.FromSeconds(Math.Max(60, _settings.LiveLocationTtlSeconds));

        var batch = db.CreateBatch();
        var setTask = batch.StringSetAsync(memberKey, payload, ttl);
        var hashTask = batch.HashSetAsync(tripKey, update.UserId, payload);
        var expireTask = batch.KeyExpireAsync(tripKey, ttl);
        batch.Execute();

        await Task.WhenAll(setTask, hashTask, expireTask);
        logger.LogDebug(
            "Saved live location for user {UserId} on trip {TripId} to Redis",
            update.UserId,
            update.TripId);
    }

    public async Task<LiveLocationUpdateDto?> GetAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var value = await multiplexer.GetDatabase().StringGetAsync(MemberKey(tripId, userId));
        return Deserialize(value);
    }

    public async Task<IReadOnlyList<LiveLocationUpdateDto>> GetTripLocationsAsync(
        string tripId,
        CancellationToken cancellationToken = default)
    {
        var entries = await multiplexer.GetDatabase().HashGetAllAsync(TripKey(tripId));
        if (entries.Length == 0)
        {
            return [];
        }

        return entries
            .Select(entry => Deserialize(entry.Value))
            .Where(x => x is not null)
            .Cast<LiveLocationUpdateDto>()
            .OrderByDescending(x => x.Timestamp)
            .ToList();
    }

    public async Task RemoveAsync(
        string tripId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var db = multiplexer.GetDatabase();
        var batch = db.CreateBatch();
        var deleteTask = batch.KeyDeleteAsync(MemberKey(tripId, userId));
        var hashDeleteTask = batch.HashDeleteAsync(TripKey(tripId), userId);
        batch.Execute();
        await Task.WhenAll(deleteTask, hashDeleteTask);
    }

    private string MemberKey(string tripId, string userId) =>
        $"{_settings.KeyPrefix}:live:{tripId}:{userId}";

    private string TripKey(string tripId) =>
        $"{_settings.KeyPrefix}:live:trip:{tripId}";

    private static LiveLocationUpdateDto? Deserialize(RedisValue value)
    {
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        return JsonSerializer.Deserialize<LiveLocationUpdateDto>((string)value!, JsonOptions);
    }
}
