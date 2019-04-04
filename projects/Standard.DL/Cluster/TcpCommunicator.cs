using System.Net;
using System.Net.Sockets;

namespace Std.DL.Cluster;

/// <summary>
///     TCP 通信器 —— 基于 TCP Socket 的跨节点集合通信实现
///     使用简单的长度前缀协议：[4 字节长度][payload 字节]
///     AllReduce 实现为 Reduce-Scatter + All-Gather
/// </summary>
public sealed class TcpCommunicator : ICollectiveCommunicator
{
    private readonly Dictionary<int, TcpClient> _clients;
    private readonly Dictionary<int, TcpListener> _listeners;
    private readonly object _lock;
    private readonly Dictionary<int, NetworkStream> _streams;

    /// <summary>
    ///     创建 TCP 通信器
    /// </summary>
    /// <param name="rank">本节点排名</param>
    /// <param name="worldSize">总节点数</param>
    /// <param name="endpoints">各节点的网络端点（索引 = rank）</param>
    public TcpCommunicator(int rank, int worldSize, IPEndPoint[] endpoints)
    {
        Rank = rank;
        WorldSize = worldSize;
        _clients = new Dictionary<int, TcpClient>();
        _listeners = new Dictionary<int, TcpListener>();
        _streams = new Dictionary<int, NetworkStream>();
        _lock = new object();

        InitializeConnections(endpoints);
    }

    /// <summary>
    ///     当前节点的排名
    /// </summary>
    public int Rank { get; }

    /// <summary>
    ///     参与通信的总节点数
    /// </summary>
    public int WorldSize { get; }

    /// <summary>
    ///     全局规约 —— 实现为 Reduce-Scatter + All-Gather
    /// </summary>
    /// <param name="data">本节点参与规约的数据</param>
    /// <param name="op">规约操作类型</param>
    public async Task AllReduceAsync(float[] data, ReduceOp op)
    {
        await ReduceScatterAsync(data, op).ConfigureAwait(false);

        var chunkSize = data.Length / WorldSize;
        var sendData = new float[chunkSize];
        Array.Copy(data, 0, sendData, 0, chunkSize);

        var recvBuffer = new float[data.Length];
        await AllGatherAsync(sendData, recvBuffer).ConfigureAwait(false);

        Array.Copy(recvBuffer, data, data.Length);
    }

    /// <summary>
    ///     规约散射 —— 环形算法，每步与相邻节点交换数据块并规约
    /// </summary>
    /// <param name="data">本节点的完整数据</param>
    /// <param name="op">规约操作类型</param>
    public async Task ReduceScatterAsync(float[] data, ReduceOp op)
    {
        var chunkSize = data.Length / WorldSize;
        var buffer = new float[chunkSize];

        for (var step = 0; step < WorldSize - 1; step++)
        {
            var sendRank = (Rank + 1) % WorldSize;
            var recvRank = (Rank - 1 + WorldSize) % WorldSize;

            var sendChunkIdx = (Rank - step + WorldSize) % WorldSize;
            var sendOffset = sendChunkIdx * chunkSize;

            var sendData = new float[chunkSize];
            Array.Copy(data, sendOffset, sendData, 0, chunkSize);

            var sendTask = SendAsync(sendData, sendRank);
            var recvTask = RecvAsync(buffer, recvRank);

            await Task.WhenAll(sendTask, recvTask).ConfigureAwait(false);

            var reduceChunkIdx = (Rank - step - 1 + WorldSize) % WorldSize;
            var reduceOffset = reduceChunkIdx * chunkSize;

            ApplyReduceInPlace(data, reduceOffset, buffer, chunkSize, op);
        }
    }

    /// <summary>
    ///     全局收集 —— 环形算法，每步与相邻节点交换数据块
    /// </summary>
    /// <param name="sendData">本节点发送的数据</param>
    /// <param name="recvBuffer">接收缓冲区</param>
    public async Task AllGatherAsync(float[] sendData, float[] recvBuffer)
    {
        var chunkSize = sendData.Length;
        Array.Copy(sendData, 0, recvBuffer, Rank * chunkSize, chunkSize);

        var buffer = new float[chunkSize];

        for (var step = 0; step < WorldSize - 1; step++)
        {
            var sendRank = (Rank + 1) % WorldSize;
            var recvRank = (Rank - 1 + WorldSize) % WorldSize;

            var sendChunkIdx = (Rank - step + WorldSize) % WorldSize;
            var sendOffset = sendChunkIdx * chunkSize;

            var sendChunk = new float[chunkSize];
            Array.Copy(recvBuffer, sendOffset, sendChunk, 0, chunkSize);

            var sendTask = SendAsync(sendChunk, sendRank);
            var recvTask = RecvAsync(buffer, recvRank);

            await Task.WhenAll(sendTask, recvTask).ConfigureAwait(false);

            var recvChunkIdx = (Rank - step - 1 + WorldSize) % WorldSize;
            var recvOffset = recvChunkIdx * chunkSize;
            Array.Copy(buffer, 0, recvBuffer, recvOffset, chunkSize);
        }
    }

    /// <summary>
    ///     广播 —— 从根节点向所有节点发送数据
    /// </summary>
    /// <param name="data">广播数据</param>
    /// <param name="rootRank">广播源节点</param>
    public async Task BroadcastAsync(float[] data, int rootRank)
    {
        if (Rank == rootRank)
        {
            for (var i = 0; i < WorldSize; i++)
                if (i != Rank)
                    await SendAsync(data, i).ConfigureAwait(false);
        }
        else
        {
            await RecvAsync(data, rootRank).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     点对点发送 —— 通过 TCP 流发送数据
    /// </summary>
    /// <param name="data">待发送的数据</param>
    /// <param name="destRank">目标节点</param>
    public async Task SendAsync(float[] data, int destRank)
    {
        var stream = GetStream(destRank);
        var byteData = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);

        var lengthBytes = BitConverter.GetBytes(byteData.Length);
        await stream.WriteAsync(lengthBytes, 0, 4).ConfigureAwait(false);
        await stream.WriteAsync(byteData, 0, byteData.Length).ConfigureAwait(false);
        await stream.FlushAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     点对点接收 —— 从 TCP 流接收数据
    /// </summary>
    /// <param name="buffer">接收缓冲区</param>
    /// <param name="srcRank">源节点</param>
    public async Task RecvAsync(float[] buffer, int srcRank)
    {
        var stream = GetStream(srcRank);

        var lengthBytes = new byte[4];
        await ReadExactAsync(stream, lengthBytes, 4).ConfigureAwait(false);
        var length = BitConverter.ToInt32(lengthBytes, 0);

        var byteData = new byte[length];
        await ReadExactAsync(stream, byteData, length).ConfigureAwait(false);

        Buffer.BlockCopy(byteData, 0, buffer, 0, System.Math.Min(length, buffer.Length * sizeof(float)));
    }

    /// <summary>
    ///     释放所有 TCP 连接和监听器
    /// </summary>
    public void Dispose()
    {
        foreach (var stream in _streams.Values) stream.Close();

        foreach (var client in _clients.Values) client.Close();

        foreach (var listener in _listeners.Values) listener.Stop();

        _streams.Clear();
        _clients.Clear();
        _listeners.Clear();
    }

    private void InitializeConnections(IPEndPoint[] endpoints)
    {
        for (var i = Rank + 1; i < WorldSize; i++)
        {
            var listener = new TcpListener(endpoints[Rank]);
            listener.Start();
            _listeners[i] = listener;
        }

        for (var i = 0; i < Rank; i++)
        {
            var client = new TcpClient();
            client.Connect(endpoints[i]);
            _clients[i] = client;
            _streams[i] = client.GetStream();
        }

        for (var i = Rank + 1; i < WorldSize; i++)
        {
            var listener = _listeners[i];
            var tcpClient = listener.AcceptTcpClient();
            _clients[i] = tcpClient;
            _streams[i] = tcpClient.GetStream();
        }

        foreach (var listener in _listeners.Values) listener.Stop();
        _listeners.Clear();
    }

    private NetworkStream GetStream(int rank)
    {
        lock (_lock)
        {
            return _streams[rank];
        }
    }

    private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = await stream.ReadAsync(buffer, offset, count - offset).ConfigureAwait(false);
            if (read == 0) throw new IOException($"连接关闭：已读 {offset}/{count} 字节");
            offset += read;
        }
    }

    private static void ApplyReduceInPlace(float[] data, int dataOffset, float[] other, int count, ReduceOp op)
    {
        switch (op)
        {
            case ReduceOp.Sum:
            {
                for (var i = 0; i < count; i++) data[dataOffset + i] += other[i];
                break;
            }
            case ReduceOp.Avg:
            {
                for (var i = 0; i < count; i++) data[dataOffset + i] += other[i];
                break;
            }
            case ReduceOp.Max:
            {
                for (var i = 0; i < count; i++)
                    if (other[i] > data[dataOffset + i])
                        data[dataOffset + i] = other[i];

                break;
            }
            case ReduceOp.Min:
            {
                for (var i = 0; i < count; i++)
                    if (other[i] < data[dataOffset + i])
                        data[dataOffset + i] = other[i];

                break;
            }
        }
    }
}