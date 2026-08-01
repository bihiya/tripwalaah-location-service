using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tripwalaah.LocationService.Infrastructure.Messaging;

/// <summary>
/// Optional consumer scaffold for trip events. Enable with Kafka:EnableConsumer=true.
/// </summary>
public sealed class KafkaTripEventsConsumerHostedService(
    IOptions<KafkaSettings> options,
    ILogger<KafkaTripEventsConsumerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || !settings.EnableConsumer)
        {
            logger.LogInformation("Kafka consumer is disabled");
            return;
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            ClientId = $"{settings.ClientId}-consumer",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            AllowAutoCreateTopics = settings.AllowAutoCreateTopics
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe([settings.LiveLocationTopic, settings.TripEventsTopic]);

        logger.LogInformation(
            "Kafka consumer subscribed to {Topics}",
            string.Join(", ", settings.LiveLocationTopic, settings.TripEventsTopic));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    if (result?.Message?.Value is null)
                    {
                        continue;
                    }

                    using var document = JsonDocument.Parse(result.Message.Value);
                    var eventType = document.RootElement.TryGetProperty("eventType", out var typeNode)
                        ? typeNode.GetString()
                        : "unknown";

                    logger.LogDebug(
                        "Consumed Kafka event {EventType} from {Topic} key={Key}",
                        eventType,
                        result.Topic,
                        result.Message.Key);
                }
                catch (ConsumeException ex)
                {
                    logger.LogWarning(ex, "Kafka consume error");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // shutdown
        }
        finally
        {
            consumer.Close();
        }
    }
}
