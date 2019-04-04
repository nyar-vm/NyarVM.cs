using Acorn.Jvm.Decode;

namespace Nyar.Tests.Binary.JvmTests;

/// <summary>
/// JVM ClassFile �������쳣·�����ԣ���֤���������쳣�����µ���Ϊ��
///</summary>
public class JvmDecoderErrorTests
{
    private readonly JvmDecoder _decoder = new();

    #region ħ����֤

    [Fact]
    public void Decode_InvalidMagic_ThrowsInvalidDataException()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        var ex = Assert.Throws<InvalidDataException>(() => _decoder.decode(data));
        Assert.Contains("ħ��", ex.Message);
    }

    [Fact]
    public void Decode_EmptyData_ThrowsException()
    {
        var data = Array.Empty<byte>();

        Assert.ThrowsAny<Exception>(() => _decoder.decode(data));
    }

    [Fact]
    public void Decode_PartialMagic_ThrowsException()
    {
        var data = new byte[] { 0xCA, 0xFE };

        Assert.ThrowsAny<Exception>(() => _decoder.decode(data));
    }

    #endregion

    #region �ض�����

    [Fact]
    public void Decode_TruncatedAfterMagic_ThrowsException()
    {
        var data = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };

        Assert.ThrowsAny<Exception>(() => _decoder.decode(data));
    }

    [Fact]
    public void Decode_TruncatedConstantPool_ThrowsException()
    {
        var data = new byte[]
        {
            0xCA, 0xFE, 0xBA, 0xBE,
            0x00, 0x00,
            0x00, 0x41,
            0x00, 0x10
        };

        Assert.ThrowsAny<Exception>(() => _decoder.decode(data));
    }

    #endregion

    #region ���������

    [Fact]
    public void Decode_InvalidConstantPoolTag_ThrowsException()
    {
        var data = new byte[]
        {
            0xCA, 0xFE, 0xBA, 0xBE,
            0x00, 0x00,
            0x00, 0x41,
            0x00, 0x02,
            0xFF,
            0x00
        };

        Assert.ThrowsAny<Exception>(() => _decoder.decode(data));
    }

    #endregion
}
