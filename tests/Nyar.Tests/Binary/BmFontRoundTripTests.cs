namespace Nyar.Tests.Binary;

public sealed class BmFontRoundTripTests
{
    #region 基本往返测试

    [Fact]
    public void EncodeDecode_FullFont_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 32,
                BitDepth = 8,
                Bold = true,
                Italic = false,
                CharSet = 0,
                Unicode = true,
                SpacingH = 1,
                SpacingV = 1,
                LineHeight = 32,
                FontName = "TestFont"
            },
            Common = new BmFontCommon
            {
                LineHeight = 32,
                Base = 26,
                ScaleW = 256,
                ScaleH = 256,
                Pages = 1,
                AlphaChannel = true,
                RedChannel = false,
                GreenChannel = false,
                BlueChannel = false,
                Packed = false
            },
            Pages = new List<string> { "test_0.png" },
            Chars = new List<BmFontChar>
            {
                new()
                {
                    Id = 65,
                    X = 0,
                    Y = 0,
                    Width = 20,
                    Height = 26,
                    XOffset = 0,
                    YOffset = 0,
                    XAdvance = 22,
                    Page = 0,
                    Channel = BmFontChannel.Glyph
                },
                new()
                {
                    Id = 66,
                    X = 22,
                    Y = 0,
                    Width = 18,
                    Height = 26,
                    XOffset = 1,
                    YOffset = 0,
                    XAdvance = 20,
                    Page = 0,
                    Channel = BmFontChannel.Glyph
                }
            },
            KerningPairs = new List<BmFontKerningPair>
            {
                new()
                {
                    First = 65,
                    Second = 66,
                    Amount = -1
                }
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.Equal(32, result.Info.Size);
        Assert.Equal(8, result.Info.BitDepth);
        Assert.True(result.Info.Bold);
        Assert.False(result.Info.Italic);
        Assert.True(result.Info.Unicode);
        Assert.Equal(1, result.Info.SpacingH);
        Assert.Equal(32, result.Info.LineHeight);
        Assert.Equal("TestFont", result.Info.FontName);

        Assert.Equal(32, result.Common.LineHeight);
        Assert.Equal(26, result.Common.Base);
        Assert.Equal(256, result.Common.ScaleW);
        Assert.Equal(256, result.Common.ScaleH);
        Assert.Equal((ushort)1, result.Common.Pages);
        Assert.True(result.Common.AlphaChannel);

        Assert.Single(result.Pages);
        Assert.Contains("test_0.png", result.Pages);

        Assert.Equal(2, result.Chars.Count);
        Assert.Equal(65u, result.Chars[0].Id);
        Assert.Equal(20, result.Chars[0].Width);
        Assert.Equal(22, result.Chars[0].XAdvance);

        Assert.Single(result.KerningPairs);
        Assert.Equal(65u, result.KerningPairs[0].First);
        Assert.Equal(66u, result.KerningPairs[0].Second);
        Assert.Equal(-1, result.KerningPairs[0].Amount);
    }

    [Fact]
    public void EncodeDecode_MinimalFont_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 16,
                BitDepth = 8,
                FontName = "Minimal"
            },
            Common = new BmFontCommon
            {
                LineHeight = 16,
                Base = 12,
                ScaleW = 128,
                ScaleH = 128,
                Pages = 1
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.Equal("Minimal", result.Info.FontName);
        Assert.Equal(16, result.Info.Size);
        Assert.Equal((ushort)1, result.Common.Pages);
    }

    [Fact]
    public void EncodeDecode_MultiplePages_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 24,
                FontName = "MultiPage"
            },
            Common = new BmFontCommon
            {
                LineHeight = 24,
                Base = 18,
                ScaleW = 512,
                ScaleH = 512,
                Pages = 3
            },
            Pages = new List<string> { "page0.png", "page1.png", "page2.png" }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.Equal(3, result.Pages.Count);
        Assert.Equal("page0.png", result.Pages[0]);
        Assert.Equal("page1.png", result.Pages[1]);
        Assert.Equal("page2.png", result.Pages[2]);
    }

    #endregion

    #region 标志位测试

    [Fact]
    public void EncodeDecode_ItalicAndUnicode_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 20,
                Italic = true,
                Unicode = true,
                FontName = "ItalicUnicode"
            },
            Common = new BmFontCommon
            {
                LineHeight = 20,
                Base = 15,
                ScaleW = 256,
                ScaleH = 256,
                Pages = 1
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.True(result.Info.Italic);
        Assert.True(result.Info.Unicode);
        Assert.False(result.Info.Bold);
    }

    [Fact]
    public void EncodeDecode_ChannelFlags_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 16,
                FontName = "RGBA"
            },
            Common = new BmFontCommon
            {
                LineHeight = 16,
                Base = 12,
                ScaleW = 128,
                ScaleH = 128,
                Pages = 1,
                AlphaChannel = true,
                RedChannel = true,
                GreenChannel = true,
                BlueChannel = true,
                Packed = true
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.True(result.Common.AlphaChannel);
        Assert.True(result.Common.RedChannel);
        Assert.True(result.Common.GreenChannel);
        Assert.True(result.Common.BlueChannel);
        Assert.True(result.Common.Packed);
    }

    #endregion

    #region Scanner 验证

    [Fact]
    public void EncodeDecode_ScannerValidation()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 48,
                Bold = true,
                Unicode = true,
                FontName = "Scanner"
            },
            Common = new BmFontCommon
            {
                LineHeight = 48,
                Base = 36,
                ScaleW = 1024,
                ScaleH = 1024,
                Pages = 1
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var scanner = new BmFontScanner(bytes);
        Assert.True(scanner.IsBmFontBinary());

        var header = scanner.ScanHeader();
        Assert.Equal(3, header.Version);
        Assert.True(header.IsBinary);
        Assert.Equal(48, header.FontSize);
        Assert.True(header.Bold);
        Assert.True(header.Unicode);
    }

    #endregion

    #region 字符通道测试

    [Fact]
    public void EncodeDecode_CharChannels_Roundtrip()
    {
        var original = new BmFontData
        {
            Info = new BmFontInfo
            {
                Size = 16,
                FontName = "Channels"
            },
            Common = new BmFontCommon
            {
                LineHeight = 16,
                Base = 12,
                ScaleW = 128,
                ScaleH = 128,
                Pages = 1
            },
            Chars = new List<BmFontChar>
            {
                new() { Id = 32, X = 0, Y = 0, Width = 4, Height = 0, XAdvance = 4, Channel = BmFontChannel.Outline },
                new()
                {
                    Id = 33, X = 5, Y = 0, Width = 8, Height = 12, XAdvance = 10,
                    Channel = BmFontChannel.GlyphAndOutline
                },
                new() { Id = 34, X = 14, Y = 0, Width = 8, Height = 12, XAdvance = 10, Channel = BmFontChannel.Zero }
            }
        };

        var encoder = new BmFontEncoder();
        var bytes = encoder.Encode(original);

        var result = new BmFontDecoder(bytes).Decode();

        Assert.Equal(3, result.Chars.Count);
        Assert.Equal(BmFontChannel.Outline, result.Chars[0].Channel);
        Assert.Equal(BmFontChannel.GlyphAndOutline, result.Chars[1].Channel);
        Assert.Equal(BmFontChannel.Zero, result.Chars[2].Channel);
    }

    #endregion
}
