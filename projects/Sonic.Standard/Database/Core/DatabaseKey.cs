using System.Text.Json;

namespace Std.Database.Core;

/// <summary>
///     LightDB 键，支持任意可序列化类型
/// </summary>
public readonly struct DatabaseKey : IComparable<DatabaseKey>, IEquatable<DatabaseKey>
{
    private readonly byte[] _bytes;

    public DatabaseKey(byte[] bytes)
    {
        _bytes = bytes;
    }

    public ReadOnlyMemory<byte> bytes => _bytes;

    public int length => _bytes?.Length ?? 0;

    public bool is_empty => _bytes is null || _bytes.Length == 0;

    public static DatabaseKey empty => new([]);

    /// <summary>
    ///     最大键哨兵值，用于范围扫描的上界（1024 字节 0xFF）
    /// </summary>
    public static DatabaseKey max_value => new(MaxKeyBytes._value);

    private static class MaxKeyBytes
    {
        internal static readonly byte[] _value = init_max_key();

        private static byte[] init_max_key()
        {
            var bytes = new byte[1024];
            Array.Fill(bytes, (byte)0xFF);
            return bytes;
        }
    }

    public static DatabaseKey from_string(string value)
    {
        return new DatabaseKey(Encoding.UTF8.GetBytes(value));
    }

    public static DatabaseKey from_u_int64(ulong value)
    {
        return new DatabaseKey(BitConverter.GetBytes(value));
    }

    public static DatabaseKey from_guid(Guid value)
    {
        return new DatabaseKey(value.ToByteArray());
    }

    public static DatabaseKey from_object<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        return from_string(json);
    }

    public static implicit operator DatabaseKey(string value)
    {
        return from_string(value);
    }

    public static implicit operator DatabaseKey(int value)
    {
        return from_u_int64((ulong)value);
    }

    public static implicit operator DatabaseKey(long value)
    {
        return from_u_int64((ulong)value);
    }

    public static implicit operator DatabaseKey(ulong value)
    {
        return from_u_int64(value);
    }

    public static implicit operator DatabaseKey(Guid value)
    {
        return from_guid(value);
    }

    public static implicit operator DatabaseKey(byte[] value)
    {
        return new DatabaseKey(value);
    }

    public bool starts_with(DatabaseKey prefix)
    {
        if (prefix.length > length) return false;

        return SimdKeyComparison.starts_with_simd(bytes.Span, prefix.bytes.Span);
    }

    public int CompareTo(DatabaseKey other)
    {
        return SimdKeyComparison.compare_to_simd(bytes.Span, other.bytes.Span);
    }

    public bool Equals(DatabaseKey other)
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
        return obj is DatabaseKey other && Equals(other);
    }

    public override string ToString()
    {
        if (_bytes is null || _bytes.Length == 0) return "";

        try
        {
            return Encoding.UTF8.GetString(_bytes);
        }
        catch
        {
            return Convert.ToHexString(_bytes);
        }
    }
}