using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Hermes.Stream.RabbitMQ;

/// <summary>
///     RabbitMQ 消息流服务 — 基于自研 AMQP 0-9-1 协议实现，零外部依赖
/// </summary>
public sealed class RabbitMqStreamService : IStreamService, IDisposable
{
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _consumerCts = new();
    private readonly RabbitMqOptions _options;
    private readonly ConcurrentDictionary<string, ConcurrentBag<SubscriberEntry>> _subscribers = new();
    private ushort _channelCounter;
    private RabbitMqConnection? _connection;
    private bool _disposed;

    public RabbitMqStreamService(StreamOptions options)
    {
        _options = ParseOptions(options);
    }

    public RabbitMqStreamService(RabbitMqOptions options)
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

        _connection?.Dispose();
        _consumerCts.Clear();
        _subscribers.Clear();
        _connectionLock.Dispose();
    }

    public async Task PublishAsync<T>(string topic, T message, string? key = null, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var (exchange, routingKey) = ParseTopic(topic);
        var conn = await GetOrCreateConnectionAsync(ct);
        var channel = await GetOrCreateChannelAsync(conn, ct);

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await conn.DeclareExchangeAsync(channel, exchange, "topic", true, ct);
        await conn.PublishAsync(channel, exchange, routingKey, body, ct);
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
        try
        {
            var (exchange, routingKey) = ParseTopic(fullTopic);
            var conn = await GetOrCreateConnectionAsync(ct);
            var channel = await GetOrCreateChannelAsync(conn, ct);

            var queueName = $"hermes.{exchange}.{routingKey.Replace('*', '_').Replace('#', '_')}";

            await conn.DeclareExchangeAsync(channel, exchange, "topic", true, ct);
            await conn.DeclareQueueAsync(channel, queueName, true, ct);
            await conn.BindQueueAsync(channel, queueName, exchange, routingKey, ct);
            await conn.ConsumeAsync(channel, queueName, ct);

            while (!ct.IsCancellationRequested)
            {
                var frame = await conn.ReadFrameAsync(ct);

                if (frame.type == AmqpConstants.FrameMethod &&
                    frame.ReadClassId() == AmqpConstants.BasicClass &&
                    frame.ReadMethodId() == AmqpConstants.BasicDeliverMethod)
                {
                    if (_subscribers.TryGetValue(fullTopic, out var bag))
                    {
                        var json = ExtractBodyFromDeliverFrame(frame);

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
                    }
                }
                else if (frame.type == AmqpConstants.FrameHeartbeat)
                {
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
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

    private async Task<RabbitMqConnection> GetOrCreateConnectionAsync(CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_connection?.IsConnected == true) return _connection;

            _connection?.Dispose();
            _connection = new RabbitMqConnection(_options);
            await _connection.ConnectAsync(ct);
            _channelCounter = 0;
            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<ushort> GetOrCreateChannelAsync(RabbitMqConnection conn, CancellationToken ct)
    {
        var channel = ++_channelCounter;
        await conn.OpenChannelAsync(channel, ct);
        return channel;
    }

    private static (string exchange, string routingKey) ParseTopic(string topic)
    {
        var parts = topic.Split('.', 2);
        if (parts.Length == 1) return (parts[0], "#");

        return (parts[0], parts[1]);
    }

    private static string ExtractBodyFromDeliverFrame(AmqpFrame frame)
    {
        try
        {
            var payload = frame.Payload;
            var offset = 4;

            var consumerTagLen = payload[offset + 1];
            offset += 2 + consumerTagLen;

            if (offset + 8 <= payload.Length) offset += 8;

            return Encoding.UTF8.GetString(payload, Math.Min(offset, payload.Length),
                Math.Max(0, payload.Length - offset));
        }
        catch
        {
            return "";
        }
    }

    private static RabbitMqOptions ParseOptions(StreamOptions options)
    {
        var connStr = options.ConnectionString ?? options.BootstrapServers ?? "localhost:5672";

        if (connStr.StartsWith("amqp://"))
        {
            var uri = new Uri(connStr);
            var userInfo = string.IsNullOrEmpty(uri.UserInfo) ? "guest" : uri.UserInfo;
            var parts = userInfo.Split(':', 2);

            return new RabbitMqOptions
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 5672,
                Username = parts[0],
                Password = parts.Length > 1 ? parts[1] : "guest",
                VirtualHost = Uri.UnescapeDataString(uri.AbsolutePath) ?? "/"
            };
        }

        var hostPort = connStr.Split(':');
        return new RabbitMqOptions
        {
            Host = hostPort[0],
            Port = hostPort.Length > 1 && int.TryParse(hostPort[1], out var port) ? port : 5672
        };
    }

    private string BuildTopic(string topic)
    {
        return topic;
    }

    #endregion
}