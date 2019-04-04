namespace Std.DataProcess.Serialize;

/// <summary>
///     `SerdeValueSerializer` 的元组子写入器。
///     在 `SerdeValue` 中元组按数组表示，因此直接复用数组收尾语义。
/// </summary>
public sealed class SerdeTupleSerializer : ITupleSerializer
{
    private readonly SerdeValueSerializer _serializer;
    private readonly List<SerdeValue> _elements;
    private bool _disposed;

    public SerdeTupleSerializer(SerdeValueSerializer serializer, List<SerdeValue> elements)
    {
        _serializer = serializer;
        _elements = elements;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _serializer.Pop();
        _disposed = true;
    }
}
