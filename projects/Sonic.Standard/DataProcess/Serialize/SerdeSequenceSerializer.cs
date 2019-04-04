namespace Std.DataProcess.Serialize;

/// <summary>
///     `SerdeValueSerializer` 的数组子写入器。
///     元素值会先落到共享 serializer，再在结束时统一折叠为数组。
/// </summary>
public sealed class SerdeSequenceSerializer : ISequenceSerializer
{
    private readonly SerdeValueSerializer _serializer;
    private readonly List<SerdeValue> _elements;
    private bool _disposed;

    public SerdeSequenceSerializer(SerdeValueSerializer serializer, List<SerdeValue> elements)
    {
        _serializer = serializer;
        _elements = elements;
    }

    public void write_element(ISerializer serializer)
    {
        if (!ReferenceEquals(serializer, _serializer))
        {
            throw new InvalidOperationException("`SerdeSequenceSerializer` 仅支持共享的 `SerdeValueSerializer`。");
        }
    }

    public void end()
    {
        if (_disposed)
        {
            return;
        }

        _serializer.Pop();
        _disposed = true;
    }

    public void Dispose()
    {
        end();
    }
}
