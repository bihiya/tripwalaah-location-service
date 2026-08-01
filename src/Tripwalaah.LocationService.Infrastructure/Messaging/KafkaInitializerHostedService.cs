using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tripwalaah.LocationService.Infrastructure.Messaging;

/// <summary>
/// Validates Kafka connectivity on startup and ensures required topics exist.
/// </summary>
public sealed class KafkaInitializerHostedService(
    IOptions<KafkaSettings> options,
    ILogger<KafkaInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Kafka is disabled; skipping initializer");
            return;
        }

        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = settings.BootstrapServers,
                SocketTimeoutMs = settings.MessageTimeoutMs,
                ClientId = $"{settings.ClientId}-admin"
            }).Build();

            // Force a metadata fetch to confirm broker reachability.
            var metadata = admin.GetMetadata(TimeSpan.FromMilliseconds(settings.MessageTimeoutMs));
            logger.LogInformation(
                "Connected to Kafka cluster with {BrokerCount} broker(s) at {BootstrapServers}",
                metadata.Brokers.Count,
                settings.BootstrapServers);

            var requiredTopics = new[]
            {
                settings.LiveLocationTopic,
                settings.TripEventsTopic
            }.Distinct(StringComparer.Ordinal).ToArray();

            var existing = metadata.Topics.Select(t => t.Topic).ToHashSet(StringComparer.Ordinal);
            var missing = requiredTopics.Where(t => !existing.Contains(t)).ToArray();

            if (missing.Length == 0)
            {
                logger.LogInformation("Kafka topics already present: {Topics}", string.Join(", ", requiredTopics));
                return;
            }

            if (!settings.AllowAutoCreateTopics)
            {
                logger.LogWarning(
                    "Missing Kafka topics and auto-create is disabled: {Topics}",
                    string.Join(", ", missing));
                return;
            }

            await admin.CreateTopicsAsync(
                missing.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 3,
                    ReplicationFactor = 1
                }));

            logger.LogInformation("Created Kafka topics: {Topics}", string.Join(", ", missing));
        }
        catch (CreateTopicsException ex) when (ex.Results.All(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            logger.LogInformation("Kafka topics already exist");
        }
        catch (Exception ex)
        {
            // Don't crash the API if Kafka is temporarily unavailable.
            logger.LogWarning(
                ex,
                "Kafka initializer could not complete against {BootstrapServers}. Publishing will retry later.",
                settings.BootstrapServers);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
