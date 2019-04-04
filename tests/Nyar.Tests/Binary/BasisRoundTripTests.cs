namespace Nyar.Tests.Binary;

public sealed class BasisRoundTripTests
{
    #region KTX2 基本往返测�?
    [Fact]
    public void EncodeDecode_Ktx2Basic_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 512,
            Height = 256,
            MipLevels = 4,
            ImageCount = 1,
            Format = BasisTextureFormat.UASTC
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(512, result.Width);
        Assert.Equal(256, result.Height);
        Assert.Equal(4, result.MipLevels);
        Assert.Equal(1, result.ImageCount);
    }

    [Fact]
    public void EncodeDecode_Ktx2LargeTexture_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 4096,
            Height = 4096,
            MipLevels = 1,
            ImageCount = 6
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(4096, result.Width);
        Assert.Equal(4096, result.Height);
        Assert.Equal(1, result.MipLevels);
        Assert.Equal(6, result.ImageCount);
    }

    [Fact]
    public void EncodeDecode_Ktx2MipLevels_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 256,
            Height = 256,
            MipLevels = 8,
            ImageCount = 1
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(8, result.MipLevels);
    }

    #endregion

    #region KTX2 带数据往返测�?
    [Fact]
    public void EncodeDecode_Ktx2WithDfd_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 128,
            Height = 128,
            MipLevels = 1,
            ImageCount = 1
        };

        var dfdData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var encoder = new BasisEncoder();
        var bytes = encoder.EncodeWithData(original, dfdData, null);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(128, result.Width);
        Assert.Equal(128, result.Height);
    }

    [Fact]
    public void EncodeDecode_Ktx2WithLevelData_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 64,
            Height = 64,
            MipLevels = 3,
            ImageCount = 1
        };

        var levelData = new byte[]?[]
        {
            [0x01, 0x02],
            [0x03],
            [0x04, 0x05, 0x06]
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.EncodeWithData(original, null, levelData);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(64, result.Width);
        Assert.Equal(64, result.Height);
        Assert.Equal(3, result.MipLevels);
    }

    #endregion

    #region 最小文件测�?
    [Fact]
    public void EncodeDecode_MinimalKtx2_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 1,
            Height = 1,
            MipLevels = 1,
            ImageCount = 1
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);

        Assert.Equal(80, bytes.Length); // 仅头�?        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
    }

    #endregion

    #region ImageCount 测试

    [Fact]
    public void EncodeDecode_Cubemap_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 256,
            Height = 256,
            MipLevels = 1,
            ImageCount = 6
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);
        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(6, result.ImageCount);
    }

    [Fact]

    public void EncodeDecode_ArrayTexture_Roundtrip()
    {
        var original = new BasisFileData
        {
            Width = 128,
            Height = 128,
            MipLevels = 1,
            ImageCount = 32
        };

        var encoder = new BasisEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new BasisDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(32, result.ImageCount);
    }

    #endregion
}

