namespace Nyar.Tests.Binary;

public sealed class ExrRoundTripTests
{
    #region »ù±¾Íù·µ²âÊÔ

    [Fact]
    public void EncodeDecode_SingleChannel_Roundtrip()
    {
        var original = new ExrImageData
        {
            Channels = new List<ExrChannel>
            {
                new() { Name = "R", PixelType = ExrPixelType.Half }
            },
            Compression = ExrCompression.Zip,
            DisplayWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 1919, YMax = 1079 },
            DataWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 1919, YMax = 1079 }
        };

        var encoder = new ExrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ExrDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1920, result.Width);
        Assert.Equal(1080, result.Height);
        Assert.Equal(ExrCompression.Zip, result.Compression);
        Assert.Single(result.Channels);
        Assert.Equal("R", result.Channels[0].Name);
        Assert.Equal(ExrPixelType.Half, result.Channels[0].PixelType);
    }

    [Fact]
    public void EncodeDecode_RGB_Roundtrip()
    {
        var original = new ExrImageData
        {
            Channels = new List<ExrChannel>
            {
                new() { Name = "R", PixelType = ExrPixelType.Float },
                new() { Name = "G", PixelType = ExrPixelType.Float },
                new() { Name = "B", PixelType = ExrPixelType.Float }
            },
            Compression = ExrCompression.Piz,
            DisplayWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 1023, YMax = 767 },
            DataWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 1023, YMax = 767 }
        };

        var encoder = new ExrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ExrDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1024, result.Width);
        Assert.Equal(768, result.Height);
        Assert.Equal(3, result.Channels.Count);
        Assert.Equal("R", result.Channels[0].Name);
        Assert.Equal("G", result.Channels[1].Name);
        Assert.Equal("B", result.Channels[2].Name);
    }

    [Fact]
    public void EncodeDecode_CroppedWindow_Roundtrip()
    {
        var original = new ExrImageData
        {
            Channels = new List<ExrChannel>
            {
                new() { Name = "Y", PixelType = ExrPixelType.Uint }
            },
            Compression = ExrCompression.None,
            DisplayWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 511, YMax = 511 },
            DataWindow = new ExrBox2i { XMin = 100, YMin = 50, XMax = 411, YMax = 461 }
        };

        var encoder = new ExrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ExrDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);
        Assert.Equal(100, result.DataWindow.XMin);
        Assert.Equal(50, result.DataWindow.YMin);
    }

    [Fact]
    public void EncodeDecode_RGBA_Roundtrip()
    {
        var original = new ExrImageData
        {
            Channels = new List<ExrChannel>
            {
                new() { Name = "R", PixelType = ExrPixelType.Half },
                new() { Name = "G", PixelType = ExrPixelType.Half },
                new() { Name = "B", PixelType = ExrPixelType.Half },
                new() { Name = "A", PixelType = ExrPixelType.Half }
            },
            Compression = ExrCompression.B44A,
            DisplayWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 2047, YMax = 1535 },
            DataWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 2047, YMax = 1535 }
        };

        var encoder = new ExrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ExrDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(2048, result.Width);
        Assert.Equal(1536, result.Height);
        Assert.Equal(4, result.Channels.Count);
        Assert.Equal(ExrCompression.B44A, result.Compression);
    }

    [Fact]
    public void EncodeDecode_Minimal_Roundtrip()
    {
        var original = new ExrImageData
        {
            Channels = new List<ExrChannel>
            {
                new() { Name = "Z", PixelType = ExrPixelType.Float }
            },
            Compression = ExrCompression.None,
            DisplayWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 0, YMax = 0 },
            DataWindow = new ExrBox2i { XMin = 0, YMin = 0, XMax = 0, YMax = 0 }
        };

        var encoder = new ExrEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ExrDecoder(bytes);
        var result = decoder.Decode();

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.Single(result.Channels);
    }

    #endregion
}
