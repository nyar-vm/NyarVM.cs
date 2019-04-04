namespace Nyar.Tests.Binary;

public class PsdScannerTests
{
    private static byte[] build_minimal_psd(int width = 100, int height = 100, ushort channels = 3, ushort depth = 8,
        ushort colorMode = 3)
    {
        var encoder = new PsdEncoder();
        var data = new PsdImageData
        {
            Width = width,
            Height = height,
            Channels = channels,
            Depth = depth,
            ColorMode = colorMode,
            Layers = [],
            MergedImageData = []
        };
        return encoder.Encode(data);
    }

    [Fact]
    public void ScanHeader_MinimalPsd_ReturnsCorrectHeader()
    {
        var bytes = build_minimal_psd(200, 150, 3, 8, 3);
        var scanner = new PsdScanner(bytes);

        var header = scanner.ScanHeader();

        Assert.Equal(1, header.Version);
        Assert.Equal(3, header.Channels);
        Assert.Equal(150, header.Height);
        Assert.Equal(200, header.Width);
        Assert.Equal((ushort)8, header.Depth);
        Assert.Equal((ushort)3, header.ColorMode);
    }

    [Fact]
    public void ScanHeader_InvalidMagic_ThrowsInvalidDataException()
    {
        var bytes = new byte[26];
        var scanner = new PsdScanner(bytes);
        var threw = false;

        try
        {
            scanner.ScanHeader();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ScanHeader_DataTooShort_ThrowsInvalidDataException()
    {
        var bytes = new byte[10];
        var scanner = new PsdScanner(bytes);
        var threw = false;

        try
        {
            scanner.ScanHeader();
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        Assert.True(threw);
    }

    [Fact]
    public void ScanHeader_ColorModeName_Rgb()
    {
        var bytes = build_minimal_psd(100, 100, 3, 8, (ushort)PsdColorMode.Rgb);
        var scanner = new PsdScanner(bytes);

        var header = scanner.ScanHeader();

        Assert.Equal("RGB", header.ColorModeName);
    }

    [Fact]
    public void ScanHeader_ColorModeName_Cmyk()
    {
        var bytes = build_minimal_psd(100, 100, 4, 8, (ushort)PsdColorMode.Cmyk);
        var scanner = new PsdScanner(bytes);

        var header = scanner.ScanHeader();

        Assert.Equal("CMYK", header.ColorModeName);
    }

    [Fact]
    public void ScanHeader_ColorModeName_Grayscale()
    {
        var bytes = build_minimal_psd(100, 100, 1, 8, (ushort)PsdColorMode.Grayscale);
        var scanner = new PsdScanner(bytes);

        var header = scanner.ScanHeader();

        Assert.Equal("灰度", header.ColorModeName);
    }

    [Fact]
    public void ScanLayerNames_NoLayers_ReturnsEmptyList()
    {
        var bytes = build_minimal_psd();
        var scanner = new PsdScanner(bytes);

        var names = scanner.ScanLayerNames();

        Assert.Empty(names);
    }

    [Fact]
    public void ScanLayerCount_NoLayers_ReturnsZero()
    {
        var bytes = build_minimal_psd();
        var scanner = new PsdScanner(bytes);

        var count = scanner.ScanLayerCount();

        Assert.Equal(0, count);
    }
}

public class PsdEncoderDecoderRoundTripTests
{
    [Fact]
    public void RoundTrip_MinimalPsd_NoLayers()
    {
        var original = new PsdImageData
        {
            Width = 100,
            Height = 100,
            Channels = 3,
            Depth = 8,
            ColorMode = 3,
            Layers = [],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PsdDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(original.Width, decoded.Width);
        Assert.Equal(original.Height, decoded.Height);
        Assert.Equal(original.Channels, decoded.Channels);
        Assert.Equal(original.Depth, decoded.Depth);
        Assert.Equal(original.ColorMode, decoded.ColorMode);
        Assert.Empty(decoded.Layers);
    }

    [Fact]
    public void RoundTrip_PsdWithSingleLayer()
    {
        var original = new PsdImageData
        {
            Width = 64,
            Height = 64,
            Channels = 4,
            Depth = 8,
            ColorMode = 3,
            Layers =
            [
                new PsdLayer
                {
                    Name = "Background",
                    Bounds = (0, 0, 64, 64),
                    ChannelCount = 4,
                    BlendMode = "norm",
                    Opacity = 255,
                    IsVisible = true
                }
            ],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PsdDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(original.Width, decoded.Width);
        Assert.Equal(original.Height, decoded.Height);
        Assert.Equal(original.Channels, decoded.Channels);
        Assert.Equal(original.Depth, decoded.Depth);
        Assert.Equal(original.ColorMode, decoded.ColorMode);
        Assert.Single(decoded.Layers);
        Assert.Equal("Background", decoded.Layers[0].Name);
        Assert.Equal("norm", decoded.Layers[0].BlendMode);
        Assert.Equal((byte)255, decoded.Layers[0].Opacity);
        Assert.True(decoded.Layers[0].IsVisible);
    }

    [Fact]
    public void RoundTrip_PsdWithMultipleLayers()
    {
        var original = new PsdImageData
        {
            Width = 256,
            Height = 256,
            Channels = 3,
            Depth = 8,
            ColorMode = 3,
            Layers =
            [
                new PsdLayer
                {
                    Name = "Layer 1",
                    Bounds = (0, 0, 256, 256),
                    ChannelCount = 3,
                    BlendMode = "norm",
                    Opacity = 255,
                    IsVisible = true
                },
                new PsdLayer
                {
                    Name = "Layer 2",
                    Bounds = (10, 10, 200, 200),
                    ChannelCount = 3,
                    BlendMode = "mult",
                    Opacity = 200,
                    IsVisible = false
                }
            ],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PsdDecoder(bytes);
        var decoded = decoder.Decode();

        Assert.Equal(2, decoded.Layers.Count);
        Assert.Equal("Layer 1", decoded.Layers[0].Name);
        Assert.Equal("Layer 2", decoded.Layers[1].Name);
        Assert.Equal("mult", decoded.Layers[1].BlendMode);
        Assert.Equal((byte)200, decoded.Layers[1].Opacity);
        Assert.False(decoded.Layers[1].IsVisible);
    }

    [Fact]
    public void RoundTrip_LayerBounds()
    {
        var original = new PsdImageData
        {
            Width = 100,
            Height = 100,
            Channels = 3,
            Depth = 8,
            ColorMode = 3,
            Layers =
            [
                new PsdLayer
                {
                    Name = "Cropped",
                    Bounds = (10, 20, 80, 90),
                    ChannelCount = 3,
                    BlendMode = "norm",
                    Opacity = 255,
                    IsVisible = true
                }
            ],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PsdDecoder(bytes);
        var decoded = decoder.Decode();

        var layer = Assert.Single(decoded.Layers);
        Assert.Equal(10, layer.Bounds.Top);
        Assert.Equal(20, layer.Bounds.Left);
        Assert.Equal(80, layer.Bounds.Bottom);
        Assert.Equal(90, layer.Bounds.Right);
    }

    [Fact]
    public void RoundTrip_DifferentColorModes()
    {
        foreach (var colorMode in new[] { PsdColorMode.Rgb, PsdColorMode.Cmyk, PsdColorMode.Grayscale })
        {
            var channels = colorMode == PsdColorMode.Cmyk ? 4 : colorMode == PsdColorMode.Grayscale ? 1 : 3;
            var original = new PsdImageData
            {
                Width = 10,
                Height = 10,
                Channels = channels,
                Depth = 8,
                ColorMode = (int)colorMode,
                Layers = [],
                MergedImageData = []
            };

            var encoder = new PsdEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new PsdDecoder(bytes);
            var decoded = decoder.Decode();

            Assert.Equal((int)colorMode, decoded.ColorMode);
            Assert.Equal(channels, decoded.Channels);
        }
    }

    [Fact]
    public void RoundTrip_DifferentDepths()
    {
        foreach (var depth in new ushort[] { 1, 8, 16, 32 })
        {
            var original = new PsdImageData
            {
                Width = 10,
                Height = 10,
                Channels = 3,
                Depth = depth,
                ColorMode = 3,
                Layers = [],
                MergedImageData = []
            };

            var encoder = new PsdEncoder();
            var bytes = encoder.Encode(original);

            var decoder = new PsdDecoder(bytes);
            var decoded = decoder.Decode();

            Assert.Equal(depth, (ushort)decoded.Depth);
        }
    }
}

public class PsdDecoderHeaderTests
{
    [Fact]
    public void DecodeHeader_ReturnsCorrectValues()
    {
        var original = new PsdImageData
        {
            Width = 1920,
            Height = 1080,
            Channels = 4,
            Depth = 16,
            ColorMode = 3,
            Layers = [],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new PsdDecoder(bytes);
        var (width, height, channels, depth, colorMode) = decoder.DecodeHeader();

        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
        Assert.Equal(4, channels);
        Assert.Equal(16, depth);
        Assert.Equal(3, colorMode);
    }
}

public class PsdEncoderOutputTests
{
    [Fact]
    public void Encode_StartsWithMagicNumber()
    {
        var data = new PsdImageData
        {
            Width = 10,
            Height = 10,
            Channels = 3,
            Depth = 8,
            ColorMode = 3,
            Layers = [],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 4);
        Assert.Equal((byte)'8', bytes[0]);
        Assert.Equal((byte)'B', bytes[1]);
        Assert.Equal((byte)'P', bytes[2]);
        Assert.Equal((byte)'S', bytes[3]);
    }

    [Fact]
    public void Encode_VersionIs1()
    {
        var data = new PsdImageData
        {
            Width = 10,
            Height = 10,
            Channels = 3,
            Depth = 8,
            ColorMode = 3,
            Layers = [],
            MergedImageData = []
        };

        var encoder = new PsdEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 6);
        Assert.Equal(0, bytes[4]);
        Assert.Equal(1, bytes[5]);
    }
}

public class PsdConstantsTests
{
    [Fact]
    public void MagicNumber_Is8BPS()
    {
        Assert.Equal(4, PsdConstants.MagicNumber.Length);
        Assert.Equal((byte)0x38, PsdConstants.MagicNumber[0]);
        Assert.Equal((byte)0x42, PsdConstants.MagicNumber[1]);
        Assert.Equal((byte)0x50, PsdConstants.MagicNumber[2]);
        Assert.Equal((byte)0x53, PsdConstants.MagicNumber[3]);
    }

    [Fact]
    public void Version_Is1()
    {
        Assert.Equal(1, PsdConstants.Version);
    }

    [Fact]
    public void ExtendedLengthMarker_IsMaxUint()
    {
        Assert.Equal(0xFFFFFFFFu, PsdConstants.ExtendedLengthMarker);
    }
}
