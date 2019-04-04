using Std.Data;
using Std.DataProcess.Encode;
using Std.DataProcess.Enframe;
using Std.DataProcess.Scan;
using Std.DataProcess.Serialize;

namespace Sonic.Testing.Data;

/// <summary>
///     异常类型测试
/// </summary>
public class ExceptionTests
{
    /// <summary>
    ///     测试 EncodeException 包含消息
    /// </summary>
    [Fact]
    public void SonicEncodingException_ContainsMessage()
    {
        var ex = new EncodeException("测试编码错误");
        Assert.Equal("测试编码错误", ex.Message);
    }

    /// <summary>
    ///     测试 SerializeException 包含消息
    /// </summary>
    [Fact]
    public void SonicSerializationException_ContainsMessage()
    {
        var ex = new SerializeException("测试序列化错误");
        Assert.Equal("测试序列化错误", ex.Message);
    }

    /// <summary>
    ///     测试 FramingException 包含消息
    /// </summary>
    [Fact]
    public void SonicFramingException_ContainsMessage()
    {
        var ex = new FramingException("测试分帧错误");
        Assert.Equal("测试分帧错误", ex.Message);
    }

    /// <summary>
    ///     测试 ScanException 包含消息
    /// </summary>
    [Fact]
    public void SonicScanException_ContainsMessage()
    {
        var ex = new ScanException("测试扫描错误");
        Assert.Equal("测试扫描错误", ex.Message);
    }

    /// <summary>
    ///     测试所有异常继承自 DataException
    /// </summary>
    [Fact]
    public void AllExceptions_InheritFromSonicDataException()
    {
        Assert.True(typeof(EncodeException).IsSubclassOf(typeof(DataException)));
        Assert.True(typeof(SerializeException).IsSubclassOf(typeof(DataException)));
        Assert.True(typeof(FramingException).IsSubclassOf(typeof(DataException)));
        Assert.True(typeof(ScanException).IsSubclassOf(typeof(DataException)));
    }
}