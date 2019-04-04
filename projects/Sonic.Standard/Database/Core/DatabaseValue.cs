using System.Text.Json;

namespace Std.Database.Core;

/// <summary>
///     LightDB 值，支持任意可序列化类型
/// </summary>
public readonly struct DatabaseValue : IEquatable<DatabaseValue>
{
    private readonly byte[] _bytes;

    /// <summary>
    ///     创建值
    /// </summary>
    /// <param name="bytes">原始字节</param>
    public DatabaseValue(byte[] bytes)
    {
        _bytes = bytes;
    }

    /// <summary>
    ///     值的字节表示
    /// </summary>
    public ReadOnlyMemory<byte> bytes => _bytes;

    /// <summary>
    ///     值的长度
    /// </summary>
    public int length => _bytes?.Length ?? 0;

    /// <summary>
    ///     是否为空
    /// </summary>
    public bool is_empty => _bytes is null || _bytes.Length == 0;

    /// <summary>
    ///     空值
    /// </summary>
    public static DatabaseValue empty => new([]);

    /// <summary>
    ///     墓碑标记值，表示键已被删除（MVCC 用）
    /// </summary>
    public static DatabaseValue tombstone => new([.. "__TOMBSTONE__"u8]);

    /// <summary>
    ///     是否为墓碑标记
    /// </summary>
    public bool is_tombstone
    {
        get
        {
            if (_bytes is null || _bytes.Length != 13) return false;

            return _bytes.AsSpan().SequenceEqual("__TOMBSTONE__"u8);
        }
    }

    /// <summary>
    ///     从字符串创建值
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>值实例</returns>
    public static DatabaseValue from_string(string value)
    {
        return new DatabaseValue(Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    ///     从 int 创建值
    /// </summary>
    /// <param name="value">int 值</param>
    /// <returns>值实例</returns>
    public static DatabaseValue from_int32(int value)
    {
        return new DatabaseValue(BitConverter.GetBytes(value));
    }

    /// <summary>
    ///     从 long 创建值
    /// </summary>
    /// <param name="value">long 值</param>
    /// <returns>值实例</returns>
    public static DatabaseValue from_int64(long value)
    {
        return new DatabaseValue(BitConverter.GetBytes(value));
    }

    /// <summary>
    ///     从 double 创建值
    /// </summary>
    /// <param name="value">double 值</param>
    /// <returns>值实例</returns>
    public static DatabaseValue from_double(double value)
    {
        return new DatabaseValue(BitConverter.GetBytes(value));
    }

    /// <summary>
    ///     从任意可序列化对象创建值（使用 JSON 序列化）
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="value">对象值</param>
    /// <returns>值实例</returns>
    public static DatabaseValue from_object<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return from_string(json);
    }

    /// <summary>
    ///     反序列化为对象
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public T? to_object<T>()
    {
        if (is_empty) return default;

        var json = Encoding.UTF8.GetString(_bytes);
        return JsonSerializer.Deserialize<T>(json);
    }

    public bool Equals(DatabaseValue other)
    {
        if (_bytes is null) return other._bytes is null;

        if (other._bytes is null) return false;

        return _bytes.AsSpan().SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        if (_bytes is null) return 0;

        var hash = new HashCode();
        hash.AddBytes(_bytes);
        return hash.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is DatabaseValue other && Equals(other);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Encoding.UTF8.GetString(_bytes);
    }
}