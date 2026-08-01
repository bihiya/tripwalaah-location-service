namespace Tripwalaah.LocationService.Infrastructure.Persistence;

public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017/tripwalaah";
    public string DatabaseName { get; set; } = "tripwalaah";
    public string LocationsCollectionName { get; set; } = "locations";
    public int MaxPoolSize { get; set; } = 10;
    public int MinPoolSize { get; set; } = 2;
    public int ConnectTimeoutMs { get; set; } = 30000;
    public int SocketTimeoutMs { get; set; } = 45000;
}
