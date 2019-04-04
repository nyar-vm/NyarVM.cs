namespace Nyar.Tests.Binary.DwarfTests;

public class DwarfRoundTripTests
{
    /// <summary>
    /// 最小编译单元往返测试：使用仅含基本字段的最的DWARFFileData 编码后解码，验证所有字段一的    
///</summary>
    [Fact]
    public void Encode_Decode_MinimalCompilationUnit()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries = []
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal((ushort)4, cu.Version);
        Assert.Equal(0u, cu.DebugInfoOffset);
        Assert.Equal((byte)8, cu.AddressSize);
        Assert.Equal((byte)0, cu.SegmentSelectorSize);
        Assert.Empty(cu.Entries);
        Assert.Empty(decoded.LineNumberTables);
    }

    /// <summary>
    ///     带条目和属性的编译单元往返测试：编译单元包含一个含多种形式属性的条目，编码后解码验证还原
    /// </summary>
    [Fact]
    public void Encode_Decode_CompilationUnitWithEntries()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 5,
                    DebugInfoOffset = 64,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x11,
                            HasChildren = true,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "test_file.gg"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x10,
                                    Form = 0x08,
                                    Value = (ulong)7
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x21,
                                    Form = 0x05,
                                    Value = true
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x3F,
                                    Form = 0x06,
                                    Value = (byte)255
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal((ushort)5, cu.Version);
        Assert.Equal(64u, cu.DebugInfoOffset);
        Assert.Equal((byte)8, cu.AddressSize);

        Assert.Single(cu.Entries);
        var entry = cu.Entries[0];
        Assert.Equal((ulong)1, entry.AbbreviationCode);
        Assert.Equal(0x11u, entry.Tag);
        Assert.True(entry.HasChildren);

        Assert.Equal(4, entry.Attributes.Count);

        var attr0 = entry.Attributes[0];
        Assert.Equal(0x03u, attr0.Name);
        Assert.Equal(0x0Bu, attr0.Form);
        Assert.Equal("test_file.gg", attr0.Value);

        var attr1 = entry.Attributes[1];
        Assert.Equal(0x10u, attr1.Name);
        Assert.Equal(0x08u, attr1.Form);
        Assert.Equal((ulong)7, attr1.Value);

        var attr2 = entry.Attributes[2];
        Assert.Equal(0x21u, attr2.Name);
        Assert.Equal(0x05u, attr2.Form);
        Assert.Equal(true, attr2.Value);

        var attr3 = entry.Attributes[3];
        Assert.Equal(0x3Fu, attr3.Name);
        Assert.Equal(0x06u, attr3.Form);
        Assert.Equal((byte)255, attr3.Value);

        Assert.Empty(decoded.LineNumberTables);
    }

    /// <summary>
    ///     多编译单元往返测试：包含多个编译单元的DWARF 文件，编码后解码验证每个单元
    /// </summary>
    [Fact]
    public void Encode_Decode_MultipleCompilationUnits()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 4,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x2E,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "main"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x49,
                                    Form = 0x0F,
                                    Value = (ulong)0x401000
                                }
                            ]
                        }
                    ]
                },
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 128,
                    AddressSize = 4,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 2,
                            Tag = 0x34,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "global_counter"
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.CompilationUnits.Count);

        var cu0 = decoded.CompilationUnits[0];
        Assert.Equal((ushort)4, cu0.Version);
        Assert.Equal(0u, cu0.DebugInfoOffset);
        Assert.Single(cu0.Entries);
        Assert.Equal(0x2Eu, cu0.Entries[0].Tag);

        var cu1 = decoded.CompilationUnits[1];
        Assert.Equal((ushort)4, cu1.Version);
        Assert.Equal(128u, cu1.DebugInfoOffset);
        Assert.Single(cu1.Entries);
        Assert.Equal(0x34u, cu1.Entries[0].Tag);

        Assert.Empty(decoded.LineNumberTables);
    }

    /// <summary>
    ///     行号表往返测试：包含编译单元和行号表的完的DWARF 文件，编码后解码验证
    /// </summary>
    [Fact]
    public void Encode_Decode_WithLineNumberTable()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries = []
                }
            ],
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 36,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = -5,
                    LineRange = 14,
                    OpcodeBase = 13,
                    StandardOpcodeLengths = [0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1],
                    FileNames = ["test.c", "header.h"]
                }
            ]
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Single(decoded.LineNumberTables);

        var table = decoded.LineNumberTables[0];
        Assert.Equal((ushort)4, table.Version);
        Assert.Equal((byte)8, table.AddressSize);
        Assert.Equal((byte)0, table.SegmentSelectorSize);
        Assert.Equal(36u, table.HeaderLength);
        Assert.Equal((byte)1, table.MinimumInstructionLength);
        Assert.Equal((byte)1, table.MaximumOperationsPerInstruction);
        Assert.Equal((byte)1, table.DefaultIsStatement);
        Assert.Equal((sbyte)(-5), table.LineBase);
        Assert.Equal((byte)14, table.LineRange);
        Assert.Equal((byte)13, table.OpcodeBase);
        Assert.Equal(12, table.StandardOpcodeLengths.Count);
        Assert.Equal([0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1], table.StandardOpcodeLengths);
        Assert.Equal(2, table.FileNames.Count);
        Assert.Equal("test.c", table.FileNames[0]);
        Assert.Equal("header.h", table.FileNames[1]);
    }

    /// <summary>
    /// 多条目的编译单元往返测试：编译单元包含多个条目，每个条目有不同的属性，编码后解码验的    
///</summary>
    [Fact]
    public void Encode_Decode_MultipleEntriesInUnit()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 5,
                    DebugInfoOffset = 256,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x11,
                            HasChildren = true,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x1B,
                                    Form = 0x0F,
                                    Value = (ulong)42
                                }
                            ]
                        },
                        new DWARFEntryData
                        {
                            AbbreviationCode = 2,
                            Tag = 0x2E,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "my_func"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x49,
                                    Form = 0x0F,
                                    Value = (ulong)0x1000
                                }
                            ]
                        },
                        new DWARFEntryData
                        {
                            AbbreviationCode = 3,
                            Tag = 0x34,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "my_var"
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];

        Assert.Equal(3, cu.Entries.Count);

        Assert.Equal((ulong)1, cu.Entries[0].AbbreviationCode);
        Assert.Equal(0x11u, cu.Entries[0].Tag);
        Assert.True(cu.Entries[0].HasChildren);
        Assert.Single(cu.Entries[0].Attributes);

        Assert.Equal((ulong)2, cu.Entries[1].AbbreviationCode);
        Assert.Equal(0x2Eu, cu.Entries[1].Tag);
        Assert.False(cu.Entries[1].HasChildren);
        Assert.Equal(2, cu.Entries[1].Attributes.Count);
        Assert.Equal("my_func", cu.Entries[1].Attributes[0].Value);

        Assert.Equal((ulong)3, cu.Entries[2].AbbreviationCode);
        Assert.Equal(0x34u, cu.Entries[2].Tag);
        Assert.False(cu.Entries[2].HasChildren);
        Assert.Single(cu.Entries[2].Attributes);
        Assert.Equal("my_var", cu.Entries[2].Attributes[0].Value);
    }

    /// <summary>
    ///     空编码数据解码测试：使用有效编码的数据，确认解码不抛异常
    /// </summary>
    [Fact]
    public void Encode_Decode_EmptyFileData()
    {
        var original = new DWARFFileData
        {
            CompilationUnits = [],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        Assert.NotNull(bytes);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.NotNull(decoded);
        Assert.Empty(decoded.CompilationUnits);
        Assert.Empty(decoded.LineNumberTables);
    }

    /// <summary>
    /// 多种属性形式往返测试：验证所有已实现的属性形式编码解码正的    
///</summary>
    [Fact]
    public void Encode_Decode_AllAttributeForms()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 5,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x11,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData { Name = 1, Form = 0x03, Value = (ushort)100 },
                                new DWARFAttributeData { Name = 2, Form = 0x04, Value = 2000u },
                                new DWARFAttributeData { Name = 3, Form = 0x05, Value = false },
                                new DWARFAttributeData { Name = 4, Form = 0x06, Value = (byte)7 },
                                new DWARFAttributeData { Name = 5, Form = 0x08, Value = (ulong)999 },
                                new DWARFAttributeData { Name = 6, Form = 0x09, Value = (ushort)300 },
                                new DWARFAttributeData { Name = 7, Form = 0x0A, Value = 4000u },
                                new DWARFAttributeData { Name = 8, Form = 0x0B, Value = "DWARF" },
                                new DWARFAttributeData { Name = 9, Form = 0x0C, Value = (byte)1 },
                                new DWARFAttributeData { Name = 10, Form = 0x0D, Value = (ushort)500 },
                                new DWARFAttributeData { Name = 11, Form = 0x0E, Value = 6000u },
                                new DWARFAttributeData { Name = 12, Form = 0x0F, Value = (ulong)7000 },
                                new DWARFAttributeData { Name = 13, Form = 0x10, Value = (ulong)8000 },
                                new DWARFAttributeData { Name = 14, Form = 0x11, Value = "ref" }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Single(cu.Entries);

        var attrs = cu.Entries[0].Attributes;
        Assert.Equal(14, attrs.Count);

        Assert.Equal(1u, attrs[0].Name);
        Assert.Equal(0x03u, attrs[0].Form);
        Assert.Equal((ushort)100, attrs[0].Value);

        Assert.Equal(2u, attrs[1].Name);
        Assert.Equal(0x04u, attrs[1].Form);
        Assert.Equal(2000u, attrs[1].Value);

        Assert.Equal(3u, attrs[2].Name);
        Assert.Equal(0x05u, attrs[2].Form);
        Assert.Equal(false, attrs[2].Value);

        Assert.Equal(4u, attrs[3].Name);
        Assert.Equal(0x06u, attrs[3].Form);
        Assert.Equal((byte)7, attrs[3].Value);

        Assert.Equal(5u, attrs[4].Name);
        Assert.Equal(0x08u, attrs[4].Form);
        Assert.Equal((ulong)999, attrs[4].Value);

        Assert.Equal(6u, attrs[5].Name);
        Assert.Equal(0x09u, attrs[5].Form);
        Assert.Equal((ushort)300, attrs[5].Value);

        Assert.Equal(7u, attrs[6].Name);
        Assert.Equal(0x0Au, attrs[6].Form);
        Assert.Equal(4000u, attrs[6].Value);

        Assert.Equal(8u, attrs[7].Name);
        Assert.Equal(0x0Bu, attrs[7].Form);
        Assert.Equal("DWARF", attrs[7].Value);

        Assert.Equal(9u, attrs[8].Name);
        Assert.Equal(0x0Cu, attrs[8].Form);
        Assert.Equal((byte)1, attrs[8].Value);

        Assert.Equal(10u, attrs[9].Name);
        Assert.Equal(0x0Du, attrs[9].Form);
        Assert.Equal((ushort)500, attrs[9].Value);

        Assert.Equal(11u, attrs[10].Name);
        Assert.Equal(0x0Eu, attrs[10].Form);
        Assert.Equal(6000u, attrs[10].Value);

        Assert.Equal(12u, attrs[11].Name);
        Assert.Equal(0x0Fu, attrs[11].Form);
        Assert.Equal((ulong)7000, attrs[11].Value);

        Assert.Equal(13u, attrs[12].Name);
        Assert.Equal(0x10u, attrs[12].Form);
        Assert.Equal((ulong)8000, attrs[12].Value);

        Assert.Equal(14u, attrs[13].Name);
        Assert.Equal(0x11u, attrs[13].Form);
        Assert.Equal("ref", attrs[13].Value);
    }
}
