namespace Nyar.Tests.Binary;

public sealed class UsdRoundTripTests
{
    #region USD 往返测试

    [Fact]
    public void EncodeDecode_Version0_Roundtrip()
    {
        var original = new UsdStageData
        {
            Version = 0,
            FileType = UsdFileType.Crate
        };

        var encoder = new UsdEncoder();
        var bytes = encoder.Encode(original);

        Assert.Equal(48, bytes.Length);

        var decoder = new UsdDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(UsdFileType.Crate, result.FileType);
        Assert.Equal(0, result.Version);
    }

    [Fact]
    public void EncodeDecode_Version1_Roundtrip()
    {
        var original = new UsdStageData
        {
            Version = 1,
            FileType = UsdFileType.Crate
        };

        var encoder = new UsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new UsdDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void Decode_InvalidMagic_Throws()
    {
        try
        {
            var decoder = new UsdDecoder("INVALID!"u8);
            decoder.Decode();
            Assert.Fail("应该抛出异常");
        }
        catch (InvalidDataException)
        {
        }
    }

    [Fact]
    public void EncodeDecode_Minimal_Roundtrip()
    {
        var original = new UsdStageData();

        var encoder = new UsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new UsdDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(UsdFileType.Crate, result.FileType);
    }

    #endregion
}
