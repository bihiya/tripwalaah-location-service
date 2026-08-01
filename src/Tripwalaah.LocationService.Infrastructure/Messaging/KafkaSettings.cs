namespace Tripwalaah.LocationService.Infrastructure.Messaging;

public sealed class KafkaSettings
{
    public const string SectionName = "Kafka";

    public bool Enabled { get; set; } = true;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "tripwalaah-location-service";
    public string GroupId { get; set; } = "tripwalaah-location-service";
    public string LiveLocationTopic { get; set; } = "tripwalaah.trip.live-location";
    public string TripEventsTopic { get; set; } = "tripwalaah.trip.events";
    public bool EnableConsumer { get; set; }
    public int MessageTimeoutMs { get; set; } = 5000;
    public string Acks { get; set; } = "all";
    public bool AllowAutoCreateTopics { get; set; } = true;
}
