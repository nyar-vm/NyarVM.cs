namespace Hermes.Stream;

public sealed class StreamOptions
{
    public string TopicPrefix { get; set; } = "hermes:";
    public string? BootstrapServers { get; set; }
    public string? ConsumerGroupId { get; set; }
    public string? ConnectionString { get; set; }
}

public interface IStreamService
{
    Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default);

    Task SubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default);

    Task UnsubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default);
}