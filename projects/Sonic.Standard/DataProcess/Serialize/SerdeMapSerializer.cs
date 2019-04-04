namespace Std.DataProcess.Serialize;

public class SerdeMapSerializer : IMapSerializer
{
    private readonly SerdeValueSerializer _serializer;
    private readonly Dictionary<string, SerdeValue> _map;
    private string? _pendingFieldName;
    private bool _disposed;

    public SerdeMapSerializer(SerdeValueSerializer serializer, Dictionary<string, SerdeValue> map)
    {
        _serializer = serializer;
        _map = map;
    }

    public void write_field_name(string name)
    {
        _pendingFieldName = name;
    }

    public void write_value(ISerializer serializer)
    {
        if (_pendingFieldName is null)
            throw new InvalidOperationException("No field name pending");

        // 假设 serializer 是同一个 SerdeValueSerializer，我们刚刚在它上面压入了一个值
        // 这个值应该是作为顶层值存在的，因为我们在调用 write_value 之前还没有压入任何容器
        var value = ((SerdeValueSerializer)serializer).Result;

        // 重新构造一个空的 serializer，否则下一个值会覆盖上一个
        // 注意：这个假设是 write_value 的调用者会再次使用同一个 serializer，但它是有状态的
        // 更好的做法是让 write_value 接受一个如何写入的函数，或者让 write_value 内部自己处理
        // 这里我们先简化，假设调用者会在 write_field_name 之后立即调用写值的代码
        // 我们这里先取上次写入的 value
        _serializer.PushField(_pendingFieldName, value);
        _pendingFieldName = null;
    }

    public void end()
    {
        if (!_disposed)
        {
            _serializer.Pop();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        end();
    }
}
