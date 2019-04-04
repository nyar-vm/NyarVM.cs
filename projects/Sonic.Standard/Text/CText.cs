using Std.Text.Ascii;
using Std.Text.Utf8;

namespace Std.Text;

public readonly struct CText
{
    private readonly byte[] _bytes;

    private CText(byte[] bytes)
    {
        _bytes = bytes;
    }

    public static CText @null => new(null!);

    public int length
    {
        get
        {
            if (_bytes == null) return 0;

            var index = index_of_null();
            return index < 0 ? _bytes.Length : index;
        }
    }

    public int byte_length => length;

    public bool is_null => _bytes == null;

    public bool is_empty => length == 0;

    public static CText from_bytes(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return @null;

        if (!bytes.AsSpan().Contains((byte)0)) throw new ArgumentException("Byte array must be null-terminated");

        return new CText(bytes);
    }

    public static CText from_bytes_unchecked(byte[] bytes)
    {
        return new CText(bytes);
    }

    public static CText from_string(string str)
    {
        var bytes = new byte[str.Length + 1];
        for (var i = 0; i < str.Length; i++)
        {
            if (str[i] > 127) throw new ArgumentException("String contains non-ASCII characters");

            bytes[i] = (byte)str[i];
        }

        bytes[str.Length] = 0;
        return new CText(bytes);
    }

    public static CText from_ascii(AsciiText ascii)
    {
        var span = ascii.as_span();
        var bytes = new byte[span.Length + 1];
        span.CopyTo(bytes);
        bytes[span.Length] = 0;
        return new CText(bytes);
    }

    public ReadOnlySpan<byte> as_span()
    {
        if (_bytes == null) return ReadOnlySpan<byte>.Empty;

        var len = index_of_null();
        return len < 0 ? _bytes.AsSpan() : _bytes.AsSpan(0, len);
    }

    public ReadOnlySpan<byte> as_span_with_null()
    {
        if (_bytes == null) return ReadOnlySpan<byte>.Empty;

        return _bytes.AsSpan();
    }

    public unsafe byte* as_pointer()
    {
        if (_bytes == null) return null;

        fixed (byte* ptr = _bytes)
        {
            return ptr;
        }
    }

    public string to_system_string()
    {
        if (_bytes == null) return string.Empty;

        var len = index_of_null();
        if (len < 0) len = _bytes.Length;

        var chars = new char[len];
        for (var i = 0; i < len; i++)
            chars[i] = (char)_bytes[i];
        return new string(chars);
    }

    public AsciiText to_ascii()
    {
        var span = as_span();
        var bytes = new byte[span.Length];
        span.CopyTo(bytes);
        return AsciiText.from_bytes(bytes);
    }

    public Utf8Text to_utf8()
    {
        return Utf8Text.from_bytes([.. as_span()]);
    }

    public override string ToString()
    {
        return to_system_string();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not CText other) return false;

        if (_bytes == null && other._bytes == null) return true;

        if (_bytes == null || other._bytes == null) return false;

        return as_span().SequenceEqual(other.as_span());
    }

    public override int GetHashCode()
    {
        if (_bytes == null) return 0;

        var hash = new HashCode();
        var span = as_span();
        foreach (var b in span) hash.Add(b);

        return hash.ToHashCode();
    }

    public static bool operator ==(CText left, CText right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CText left, CText right)
    {
        return !left.Equals(right);
    }

    public static implicit operator CText(string str)
    {
        return from_string(str);
    }

    private int index_of_null()
    {
        if (_bytes == null) return -1;

        for (var i = 0; i < _bytes.Length; i++)
            if (_bytes[i] == 0)
                return i;

        return -1;
    }
}
