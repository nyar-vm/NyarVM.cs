namespace Std.Data.Binary;

/// <summary>
///     协议扫描器基类，用于从字节流中提取协议帧
/// </summary>
public abstract class ProtocolScanner
{
    protected ProtocolScanner(byte[] data)
    {
        _data = data;
    }

    protected ProtocolScanner(ReadOnlyMemory<byte> data)
    {
        _data = data;
    }

    /// <summary>
    ///     原始协议数据
    /// </summary>
    protected ReadOnlyMemory<byte> _data { get; }

    /// <summary>
    ///     尝试读取下一条消息帧
    /// </summary>
    public abstract bool try_read_next(out ProtocolFrame frame);
}

/// <summary>
///     表示一个协议帧
/// </summary>
public readonly struct ProtocolFrame
{
    public int type { get; init; }
    public ReadOnlyMemory<byte> payload { get; init; }

    public ProtocolFrame(int type, ReadOnlyMemory<byte> payload)
    {
        this.type = type;
        this.payload = payload;
    }
}