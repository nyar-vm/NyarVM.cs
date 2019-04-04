using System.Text;

namespace Std.Data.Binary.Frame;

public static class ByteBufferStringIO
{
    public static void write_nullable_string(ref ByteBufferWriter writer, string? value)
    {
        if (value is null)
        {
            writer.write_i32_le(-1);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        writer.write_i32_le(bytes.Length);
        writer.write(bytes);
    }

    public static string? read_nullable_string(ref ByteBuffer reader)
    {
        var length = reader.read_i32_le();
        if (length < 0)
        {
            return null;
        }

        if (length == 0)
        {
            return string.Empty;
        }

        return Encoding.UTF8.GetString(reader.read_bytes(length));
    }
}
