using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tripwalaah.LocationService.Application.DTOs;
using Tripwalaah.LocationService.Application.Interfaces;

namespace Tripwalaah.LocationService.Infrastructure.Messaging;

public sealed class KafkaTripEventPublisher : ITripEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<string, string>? _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaTripEventPublisher> _logger;

    public KafkaTripEventPublisher(
        IOptions<KafkaSettings> options,
        ILogger<KafkaTripEventPublisher> logger)
    {
        _settings = options.Value;
        _logger = logger;

        if (!_settings.Enabled)
        {
            return;
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = _settings.ClientId,
            Acks = ParseAcks(_settings.Acks),
            MessageTimeoutMs = _settings.MessageTimeoutMs,
            AllowAutoCreateTopics = _settings.AllowAutoCreateTopics,
            SocketTimeoutMs = _settings.MessageTimeoutMs
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger.LogInformation(
            "Kafka producer initialized for {BootstrapServers}",
            _settings.BootstrapServers);
    }

    public Task PublishLocationUpdatedAsync(
        LiveLocationUpdateDto update,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            _settings.LiveLocationTopic,
            update.TripId,
            "location.updated",
            update,
            cancellationToken);

    public Task PublishTripStatusUpdatedAsync(
        TripStatusUpdateDto update,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            _settings.TripEventsTopic,
            update.TripId,
            "trip.status.updated",
            update,
            cancellationToken);

    public Task PublishMemberJoinedAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            _settings.TripEventsTopic,
            memberEvent.TripId,
            "trip.member.joined",
            memberEvent,
            cancellationToken);

    public Task PublishMemberLeftAsync(
        TripMemberEventDto memberEvent,
        CancellationToken cancellationToken = default) =>
        PublishAsync(
            _settings.TripEventsTopic,
            memberEvent.TripId,
            "trip.member.left",
            memberEvent,
            cancellationToken);

    public void Dispose() => _producer?.Dispose();

    private async Task PublishAsync<T>(
        string topic,
        string key,
        string eventType,
        T payload,
        CancellationToken cancellationToken)
    {
        if (_producer is null || !_settings.Enabled)
        {
            return;
        }

        var envelope = new
        {
            eventType,
            occurredAt = DateTime.UtcNow,
            source = _settings.ClientId,
            data = payload
        };

        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(envelope, JsonOptions),
                Headers =
                [
                    new Header("eventType", System.Text.Encoding.UTF8.GetBytes(eventType)),
                    new Header("contentType", System.Text.Encoding.UTF8.GetBytes("application/json"))
                ]
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogDebug(
                "Published {EventType} to {Topic} partition {Partition} offset {Offset}",
                eventType,
                topic,
                result.Partition.Value,
                result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to publish {EventType} to Kafka topic {Topic}",
                eventType,
                topic);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected Kafka publish failure for {EventType}", eventType);
        }
    }

    private static Acks ParseAcks(string value) =>
        value.ToLowerInvariant() switch
        {
            "0" or "none" => Acks.None,
            "1" or "leader" => Acks.Leader,
            _ => Acks.All
        };
}
