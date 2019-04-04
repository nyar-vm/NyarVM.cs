namespace Nyar.Tests.Binary.GnosisTests;

/// <summary>
/// Gnosis 解码器异常路径测试，验证解码器在异常输入下的行为的
///</summary>
public class GnosisDecoderErrorTests
{
    #region 魔数验证

    [Fact]
    public void Decode_InvalidMagic_ThrowsInvalidDataException()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };
        var decoder = new Nyar.Binary.Gnosis.Decode.GnosisDecoder(data);

        try
        {
            decoder.Decode();
            Assert.Fail("期望抛出 InvalidDataException");
        }
        catch (InvalidDataException ex)
        {
            Assert.Contains("魔数", ex.Message);
        }
    }

    [Fact]
    public void Decode_EmptyData_ThrowsException()
    {
        var data = Array.Empty<byte>();

        Assert.ThrowsAny<Exception>(() =>
        {
            var decoder = new Nyar.Binary.Gnosis.Decode.GnosisDecoder(data);
            decoder.Decode();
        });
    }

    [Fact]
    public void Decode_PartialMagic_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            var data = new byte[] { 0x47, 0x4E };
            var decoder = new Nyar.Binary.Gnosis.Decode.GnosisDecoder(data);
            decoder.Decode();
        });
    }

    #endregion

    #region 截断数据

    [Fact]
    public void Decode_TruncatedAfterMagic_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            var data = new byte[] { 0x47, 0x4E, 0x4F, 0x53 };
            var decoder = new Nyar.Binary.Gnosis.Decode.GnosisDecoder(data);
            decoder.Decode();
        });
    }

    [Fact]
    public void Decode_TruncatedVersion_ThrowsException()
    {
        Assert.ThrowsAny<Exception>(() =>
        {
            var data = new byte[] { 0x47, 0x4E, 0x4F, 0x53, 0x00 };
            var decoder = new Nyar.Binary.Gnosis.Decode.GnosisDecoder(data);
            decoder.Decode();
        });
    }

    #endregion

    #region 验证的

    [Fact]
    public void ValidateRaw_InvalidMagic_ReportsError()
    {
        var data = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 };

        var validator = new Nyar.Binary.Gnosis.Validate.GnosisValidator();
        var result = validator.ValidateRaw(data, out var diagnostics);

        Assert.False(result);
        Assert.Contains(diagnostics, d => d.Contains("魔数"));
    }

    [Fact]
    public void ValidateRaw_ValidMinimalModule_Passes()
    {
        var module = new GnosisModuleData
        {
            Version = GnosisConstants.CurrentVersion,
            ModuleName = "test"
        };

        var encoder = new GnosisEncoder();
        var data = encoder.Encode(module);

        var validator = new Nyar.Binary.Gnosis.Validate.GnosisValidator();
        var result = validator.ValidateRaw(data, out var diagnostics);

        Assert.True(result, string.Join("; ", diagnostics));
    }

    #endregion
}
