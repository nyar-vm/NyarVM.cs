using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Hermes.Stream.Kafka;

/// <summary>
///     Kafka 消息流服务 — 基于自研 Kafka Binary Protocol，零外部依赖
/// </summary>
public sealed class KafkaStreamService : IStreamService, IDisposable
{
    private readonly ConcurrentDictionary<string, KafkaBrokerConnection> _brokerConnections = new();
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumerCts = new();
    private readonly StreamOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentBag<SubscriberEntry>> _subscribers = new();
    private KafkaBrokerConnection? _defaultBroker;
    private bool _disposed;

    public KafkaStreamService(StreamOptions options)
    {
        _options = options;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        foreach (var kvp in _consumerCts)
        {
            kvp.Value.Cancel();
            kvp.Value.Dispose();
        }

        _defaultBroker?.Dispose();

        foreach (var conn in _brokerConnections.Values) conn.Dispose();

        _consumerCts.Clear();
        _brokerConnections.Clear();
        _subscribers.Clear();
        _connectionLock.Dispose();
    }

    public async Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var fullTopic = BuildTopic(topic);
        var broker = await GetOrCreateBrokerAsync(ct);

        var metadata = await broker.GetMetadataAsync([fullTopic], ct);

        var targetTopic = metadata.Topics.FirstOrDefault(t => t.Name == fullTopic);
        if (targetTopic == null || targetTopic.Partitions.Count == 0)
            throw new InvalidOperationException($"Kafka topic 不存在：{fullTopic}");

        var partition = 0;
        var keyBytes = key != null ? Encoding.UTF8.GetBytes(key) : [];
        if (keyBytes.Length > 0)
            partition = Math.Abs(keyBytes.Aggregate(0, (hash, b) => hash * 31 + b)) %
                        targetTopic.Partitions.Count;

        var json = JsonSerializer.Serialize(message);
        var valueBytes = Encoding.UTF8.GetBytes(json);

        var result = await broker.ProduceAsync(fullTopic, partition, keyBytes, valueBytes, ct);

        if (result.ErrorCode != 0) throw new InvalidOperationException($"Kafka Produce 失败：错误码 {result.ErrorCode}");
    }

    public Task SubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var fullTopic = BuildTopic(topic);
        var bag = _subscribers.GetOrAdd(fullTopic, _ => []);
        bag.Add(new SubscriberEntry(typeof(T), handler));

        EnsureConsumerRunning(fullTopic, ct);

        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync<T>(string topic, Action<T> handler, CancellationToken ct = default)
    {
        var fullTopic = BuildTopic(topic);

        if (_subscribers.TryGetValue(fullTopic, out var bag))
        {
            var remaining = new ConcurrentBag<SubscriberEntry>(bag.Where(e => e.Handler != (Delegate)handler));
            _subscribers.TryUpdate(fullTopic, remaining, bag);

            if (remaining.IsEmpty) StopConsumer(fullTopic);
        }

        return Task.CompletedTask;
    }

    private sealed record SubscriberEntry(Type MessageType, Delegate Handler);

    #region 消费循环

    private void EnsureConsumerRunning(string fullTopic, CancellationToken ct)
    {
        if (_consumerCts.ContainsKey(fullTopic)) return;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _consumerCts[fullTopic] = cts;

        Task.Run(() => ConsumeLoop(fullTopic, cts.Token), cts.Token);
    }

    private async Task ConsumeLoop(string fullTopic, CancellationToken ct)
    {
        var lastOffset = -1L;

        while (!ct.IsCancellationRequested)
            try
            {
                var broker = await GetOrCreateBrokerAsync(ct);
                var metadata = await broker.GetMetadataAsync([fullTopic], ct);

                var targetTopic = metadata.Topics.FirstOrDefault(t => t.Name == fullTopic);
                if (targetTopic == null || targetTopic.Partitions.Count == 0)
                {
                    await Task.Delay(1000, ct);
                    continue;
                }

                var partition = targetTopic.Partitions[0].PartitionId;
                var fetchResult = await broker.FetchAsync(fullTopic, partition, lastOffset + 1, 65536, ct);

                if (fetchResult.Records.Count > 0)
                {
                    if (_subscribers.TryGetValue(fullTopic, out var bag))
                        foreach (var record in fetchResult.Records)
                        {
                            var json = Encoding.UTF8.GetString(record.Value);

                            foreach (var entry in bag)
                                try
                                {
                                    var typedMessage = JsonSerializer.Deserialize(json, entry.MessageType);
                                    if (typedMessage != null) entry.Handler.DynamicInvoke(typedMessage);
                                }
                                catch (JsonException)
                                {
                                }
                                catch (TargetInvocationException)
                                {
                                }

                            lastOffset = record.Offset;
                        }
                }
                else
                {
                    await Task.Delay(100, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                await Task.Delay(1000, ct);
            }
    }

    private void StopConsumer(string fullTopic)
    {
        if (_consumerCts.TryRemove(fullTopic, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
        }

        _subscribers.TryRemove(fullTopic, out _);
    }

    #endregion

    #region 连接管理

    private async Task<KafkaBrokerConnection> GetOrCreateBrokerAsync(CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_defaultBroker?.IsConnected == true) return _defaultBroker;

            _defaultBroker?.Dispose();

            var servers = (_options.BootstrapServers ?? "localhost:9092").Split(',');
            var (host, port) = ParseBrokerAddress(servers[0].Trim());

            _defaultBroker = new KafkaBrokerConnection(host, port);
            await _defaultBroker.ConnectAsync(ct);
            return _defaultBroker;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private static (string host, int port) ParseBrokerAddress(string address)
    {
        var parts = address.Split(':');
        var host = parts[0];
        var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 9092;
        return (host, port);
    }

    private string BuildTopic(string topic)
    {
        return $"{_options.TopicPrefix}{topic}";
    }

    #endregion
}