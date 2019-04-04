namespace Nyar.Tests.Binary;

public sealed class CoffRoundTripTests
{
    #region 基本往返测试

    [Fact]
    public void EncodeDecode_HeaderOnly_Roundtrip()
    {
        var original = new CoffFileData
        {
            Header = new CoffHeaderData
            {
                Machine = 0x8664,
                NumberOfSections = 0,
                TimeDateStamp = 0x12345678,
                Characteristics = 0x0002
            }
        };

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(0x8664, result.Header.Machine);
        Assert.Equal((ushort)0, result.Header.NumberOfSections);
        Assert.Equal(0x12345678u, result.Header.TimeDateStamp);
        Assert.Equal((ushort)0x0002, result.Header.Characteristics);
    }

    [Fact]
    public void EncodeDecode_HeaderWithSections_Roundtrip()
    {
        var original = new CoffFileData
        {
            Header = new CoffHeaderData(),
            Sections = new List<CoffSectionHeaderData>
            {
                new()
                {
                    NameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8),
                    VirtualAddress = 0x1000,
                    SizeOfRawData = 512,
                    Characteristics = 0x60000020
                },
                new()
                {
                    NameBytes = FixedBytes8.FromSpan(".data\0\0\0"u8),
                    VirtualAddress = 0x2000,
                    SizeOfRawData = 256,
                    Characteristics = 0xC0000040
                }
            }
        };
        original.Header.Machine = 0x8664;
        original.Header.NumberOfSections = 2;
        original.Header.TimeDateStamp = 0x5678;
        original.Header.Characteristics = 0x0004;

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(2, result.Sections.Count);
        Assert.Equal(".text", result.Sections[0].Name);
        Assert.Equal(".data", result.Sections[1].Name);
        Assert.Equal(0x1000u, result.Sections[0].VirtualAddress);
        Assert.Equal(512u, result.Sections[0].SizeOfRawData);
    }

    #endregion

    #region 符号表测试

    [Fact]
    public void EncodeDecode_ShortNameSymbols_Roundtrip()
    {
        var original = new CoffFileData
        {
            Header = new CoffHeaderData(),
            Symbols = new List<CoffSymbolData>
            {
                new()
                {
                    Name = "main",
                    Value = 0x1000,
                    SectionNumber = 1,
                    Type = 0x0020,
                    StorageClass = 2
                },
                new()
                {
                    Name = "_func",
                    Value = 0x1100,
                    SectionNumber = 1,
                    Type = 0x0020,
                    StorageClass = 2
                }
            }
        };
        original.Header.Machine = 0x8664;
        original.Header.NumberOfSections = 0;
        original.Header.Characteristics = 0x0002;

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(2, result.Symbols.Count);
        Assert.Equal("main", result.Symbols[0].Name);
        Assert.Equal("_func", result.Symbols[1].Name);
        Assert.Equal(0x1000u, result.Symbols[0].Value);
        Assert.Equal(2, result.Symbols[0].StorageClass);
    }

    [Fact]
    public void EncodeDecode_LongNameSymbols_Roundtrip()
    {
        var longName = "very_long_function_name_that_exceeds_eight_chars";
        var original = new CoffFileData
        {
            Header = new CoffHeaderData(),
            Symbols = new List<CoffSymbolData>
            {
                new()
                {
                    Name = longName,
                    Value = 0x2000,
                    SectionNumber = 1,
                    Type = 0x0020,
                    StorageClass = 2
                }
            }
        };
        original.Header.Machine = 0x8664;
        original.Header.NumberOfSections = 0;
        original.Header.Characteristics = 0x0002;

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Symbols);
        Assert.Equal(longName, result.Symbols[0].Name);
    }

    #endregion

    #region 重定位测试

    [Fact]
    public void EncodeDecode_WithRelocations_Roundtrip()
    {
        var original = new CoffFileData
        {
            Header = new CoffHeaderData(),
            Sections = new List<CoffSectionHeaderData>
            {
                new()
                {
                    NameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8),
                    Characteristics = 0x60000020
                }
            },
            Relocations = new List<CoffRelocationData>
            {
                new()
                {
                    VirtualAddress = 0x1000,
                    SymbolTableIndex = 1,
                    Type = 4
                },
                new()
                {
                    VirtualAddress = 0x1010,
                    SymbolTableIndex = 2,
                    Type = 4
                }
            }
        };
        original.Header.Machine = 0x8664;
        original.Header.NumberOfSections = 1;
        original.Header.Characteristics = 0x0002;

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(2, result.Relocations.Count);
        Assert.Equal(0x1000u, result.Relocations[0].VirtualAddress);
        Assert.Equal(4, result.Relocations[0].type);
    }

    #endregion

    #region 完整往返测试

    [Fact]
    public void EncodeDecode_FullFile_Roundtrip()
    {
        var original = new CoffFileData
        {
            Header = new CoffHeaderData(),
            Sections = new List<CoffSectionHeaderData>
            {
                new()
                {
                    NameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8),
                    VirtualAddress = 0x1000,
                    SizeOfRawData = 1024,
                    Characteristics = 0x60500020
                },
                new()
                {
                    NameBytes = FixedBytes8.FromSpan(".rdata\0\0"u8),
                    VirtualAddress = 0x2000,
                    SizeOfRawData = 512,
                    Characteristics = 0x40300040
                }
            },
            Symbols = new List<CoffSymbolData>
            {
                new()
                {
                    Name = "_start",
                    Value = 0,
                    SectionNumber = 1,
                    Type = 0x0020,
                    StorageClass = 2
                },
                new()
                {
                    Name = "helper_function_with_long_name",
                    Value = 0x100,
                    SectionNumber = 1,
                    Type = 0x0020,
                    StorageClass = 2
                }
            },
            Relocations = new List<CoffRelocationData>
            {
                new()
                {
                    VirtualAddress = 0x1050,
                    SymbolTableIndex = 0,
                    Type = 4
                }
            }
        };
        original.Header.Machine = 0x8664;
        original.Header.NumberOfSections = 2;
        original.Header.TimeDateStamp = 0xDEADBEEF;
        original.Header.Characteristics = 0x0103;

        var encoder = new CoffEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new CoffDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(0x8664, result.Header.Machine);
        Assert.Equal(2, result.Sections.Count);
        Assert.Equal(2, result.Symbols.Count);
        Assert.Single(result.Relocations);
        Assert.Equal("_start", result.Symbols[0].Name);
        Assert.Equal("helper_function_with_long_name", result.Symbols[1].Name);
    }

    #endregion
}