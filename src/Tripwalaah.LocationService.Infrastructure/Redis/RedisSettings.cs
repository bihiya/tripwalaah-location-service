namespace Tripwalaah.LocationService.Infrastructure.Redis;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = "localhost:6379";
    public string KeyPrefix { get; set; } = "tripwalaah";
    public int LiveLocationTtlSeconds { get; set; } = 900;
    public bool Enabled { get; set; } = true;
}
