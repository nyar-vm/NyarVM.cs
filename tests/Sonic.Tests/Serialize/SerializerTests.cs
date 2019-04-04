using Std.Text.Utf8;

namespace Sonic.Testing.Data.Serialize;

/// <summary>
///     MessagePack 序列化器测试
/// </summary>
public class MessagePackSerializerTests
{
    /// <summary>
    ///     测试序列化 null 值
    /// </summary>
    [Fact]
    public void serialize_null_WritesNullByte()
    {
        var serializer = new MessagePackSerializer();
        serializer.serialize_null();
        var bytes = serializer.to_bytes();
        Assert.Single(bytes);
        Assert.Equal(0xC0, bytes[0]);
    }

    /// <summary>
    ///     测试序列化 bool 值
    /// </summary>
    [Fact]
    public void serialize_bool_WritesCorrectBytes()
    {
        var serializer = new MessagePackSerializer();
        serializer.serialize_bool(true);
        var bytes = serializer.to_bytes();
        Assert.Single(bytes);
        Assert.Equal(0xC3, bytes[0]);
    }

    /// <summary>
    ///     测试序列化正 FixInt
    /// </summary>
    [Fact]
    public void serialize_i32_PositiveFixInt()
    {
        var serializer = new MessagePackSerializer();
        serializer.serialize_i32(42);
        var bytes = serializer.to_bytes();
        Assert.Single(bytes);
        Assert.Equal(42, bytes[0]);
    }

    /// <summary>
    ///     测试序列化负 FixInt
    /// </summary>
    [Fact]
    public void serialize_i32_NegativeFixInt()
    {
        var serializer = new MessagePackSerializer();
        serializer.serialize_i32(-1);
        var bytes = serializer.to_bytes();
        Assert.Single(bytes);
        Assert.Equal(0xFF, bytes[0]);
    }

    /// <summary>
    ///     测试序列化 Int32 格式
    /// </summary>
    [Fact]
    public void serialize_i32_Int32Format()
    {
        var serializer = new MessagePackSerializer();
        serializer.serialize_i32(0x7FFFFFFF);
        var bytes = serializer.to_bytes();
        Assert.Equal(5, bytes.Length);
        Assert.Equal(0xD2, bytes[0]);
    }

    /// <summary>
    ///     测试序列化 FixStr
    /// </summary>
    [Fact]
    public void serialize_utf8_FixStr()
    {
        var serializer = new MessagePackSerializer();
        var text = Utf8Text.from_string("hello");
        serializer.serialize_utf8(text);
        var bytes = serializer.to_bytes();
        Assert.Equal(6, bytes.Length);
        Assert.Equal(0xA5, bytes[0]);
    }
}

/// <summary>
///     JSON 序列化器测试
/// </summary>
public class JsonSerializerTests
{
    /// <summary>
    ///     测试序列化 bool 值
    /// </summary>
    [Fact]
    public void serialize_bool_True()
    {
        using var serializer = new JsonSerializer();
        serializer.serialize_bool(true);
        var json = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("true", json);
    }

    /// <summary>
    ///     测试序列化 i32 值
    /// </summary>
    [Fact]
    public void serialize_i32()
    {
        using var serializer = new JsonSerializer();
        serializer.serialize_i32(42);
        var json = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("42", json);
    }

    /// <summary>
    ///     测试序列化带字段的 map
    /// </summary>
    [Fact]
    public void serialize_map_WithField()
    {
        using var serializer = new JsonSerializer();
        var obj = serializer.serialize_map(1);
        obj.write_field_name("key");
        serializer.serialize_i32(1);
        obj.end();
        var json = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("{\"key\":1}", json);
    }
}

/// <summary>
///     XML 序列化器测试
/// </summary>
public class XmlSerializerTests
{
    /// <summary>
    ///     测试序列化 null 值
    /// </summary>
    [Fact]
    public void serialize_null()
    {
        var serializer = new XmlSerializer();
        serializer.serialize_null();
        var xml = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("<null />", xml);
    }

    /// <summary>
    ///     测试序列化 i32 值
    /// </summary>
    [Fact]
    public void serialize_i32()
    {
        var serializer = new XmlSerializer();
        serializer.serialize_i32(42);
        var xml = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("42", xml);
    }
}

/// <summary>
///     CSV 序列化器测试
/// </summary>
public class CsvSerializerTests
{
    /// <summary>
    ///     测试序列化 i32 值
    /// </summary>
    [Fact]
    public void serialize_i32()
    {
        var serializer = new CsvSerializer();
        serializer.serialize_i32(42);
        var csv = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("42", csv);
    }

    /// <summary>
    ///     测试序列化带字段的 map
    /// </summary>
    [Fact]
    public void serialize_map_WithFields()
    {
        var serializer = new CsvSerializer();
        var obj = serializer.serialize_map(2);
        obj.write_field_name("name");
        serializer.serialize_i32(1);
        obj.write_field_name("age");
        serializer.serialize_i32(30);
        obj.end();
        var csv = Utf8Text.from_bytes_unchecked(serializer.to_utf8_bytes()).ToString();
        Assert.Equal("name1,age,30", csv);
    }
}