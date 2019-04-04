using System.Text;

namespace Hermes.Cache.Redis;

public sealed class RedisClient : IDisposable
{
    private readonly RedisConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public RedisClient(RedisConnection connection)
    {
        _connection = connection;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _lock.Dispose();
        _connection.Dispose();
    }

    public async Task<string> PingAsync(CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Ping, [], ct);
        return ExpectSimpleString(response);
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Get, [key], ct);
        return ExtractBulkString(response);
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expire = null, CancellationToken ct = default)
    {
        if (expire.HasValue)
        {
            var response = await SendCommandAsync(
                RedisConstants.Commands.Set,
                [key, value, "EX", ((int)expire.Value.TotalSeconds).ToString()],
                ct);
            ExpectSimpleString(response);
        }
        else
        {
            var response = await SendCommandAsync(
                RedisConstants.Commands.Set,
                [key, value],
                ct);
            ExpectSimpleString(response);
        }
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Del, [key], ct);
        return ExpectInteger(response) > 0;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Exists, [key], ct);
        return ExpectInteger(response) > 0;
    }

    public async Task<bool> ExpireAsync(string key, TimeSpan expire, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(
            RedisConstants.Commands.Expire,
            [key, ((int)expire.TotalSeconds).ToString()],
            ct);
        return ExpectInteger(response) > 0;
    }

    public async Task FlushDbAsync(CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.FlushDb, [], ct);
        ExpectSimpleString(response);
    }

    public async Task AuthAsync(string password, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Auth, [password], ct);
        ExpectSimpleString(response);
    }

    public async Task SelectAsync(int database, CancellationToken ct = default)
    {
        var response = await SendCommandAsync(RedisConstants.Commands.Select, [database.ToString()], ct);
        ExpectSimpleString(response);
    }

    private async Task<RedisMessageData> SendCommandAsync(string command, string[] args, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct);
        try
        {
            if (!_connection.IsConnected) await _connection.ConnectAsync(ct);

            var data = BuildCommand(command, args);
            await _connection.SendAsync(data, ct);
            return await _connection.ReceiveResponseAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static byte[] BuildCommand(string command, params string[] args)
    {
        var writer = new ByteBufferWriter(4096);
        WriteArrayHeader(ref writer, 1 + args.Length);
        WriteBulkString(ref writer, command.ToUpperInvariant());

        foreach (var arg in args) WriteBulkString(ref writer, arg);

        return writer.ToArray();
    }

    private static void WriteArrayHeader(ref ByteBufferWriter writer, int count)
    {
        writer.WriteU8((byte)RedisConstants.ArrayPrefix);
        writer.WriteString(count.ToString());
        writer.WriteU8((byte)'\r');
        writer.WriteU8((byte)'\n');
    }

    private static void WriteBulkString(ref ByteBufferWriter writer, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        writer.WriteU8((byte)RedisConstants.BulkStringPrefix);
        writer.WriteString(bytes.Length.ToString());
        writer.WriteU8((byte)'\r');
        writer.WriteU8((byte)'\n');
        writer.Write(bytes);
        writer.WriteU8((byte)'\r');
        writer.WriteU8((byte)'\n');
    }

    private static string ExpectSimpleString(RedisMessageData response)
    {
        if (response.type == RedisMessageType.Error) throw new InvalidOperationException($"Redis 错误：{response.Error}");

        if (response.type != RedisMessageType.SimpleString)
            throw new InvalidOperationException($"期望简单字符串响应，实际类型：{response.type}");

        return response.SimpleString;
    }

    private static long ExpectInteger(RedisMessageData response)
    {
        if (response.type == RedisMessageType.Error) throw new InvalidOperationException($"Redis 错误：{response.Error}");

        if (response.type != RedisMessageType.Integer)
            throw new InvalidOperationException($"期望整数响应，实际类型：{response.type}");

        return response.Integer;
    }

    private static string? ExtractBulkString(RedisMessageData response)
    {
        if (response.type == RedisMessageType.Error) throw new InvalidOperationException($"Redis 错误：{response.Error}");

        if (response.type == RedisMessageType.BulkString)
        {
            if (response.IsNullBulkString) return null;

            return Encoding.UTF8.GetString(response.BulkString);
        }

        throw new InvalidOperationException($"期望批量字符串响应，实际类型：{response.type}");
    }
}