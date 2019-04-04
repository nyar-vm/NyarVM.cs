using Nyar.Binary.Dwarf.Data;
using Nyar.Binary.Dwarf.Encode;
using Nyar.Binary.Dwarf.Decode;

namespace Nyar.Tests.Binary;

public sealed class DwarfRoundTripTests
{
    #region ��С����

    [Fact]
    public void EncodeDecode_MinimalCompilationUnit_Roundtrip()
    {
        var data = create_minimal_compilation_unit();

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Equal((ushort)4, decoded.CompilationUnits[0].Version);
        Assert.Equal((byte)8, decoded.CompilationUnits[0].AddressSize);
        Assert.Equal((byte)0, decoded.CompilationUnits[0].SegmentSelectorSize);
        Assert.Empty(decoded.LineNumberTables);
    }

    [Fact]
    public void EncodeDecode_EmptyCompilationUnit_Roundtrip()
    {
        var data = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 5,
                    DebugInfoOffset = 0,
                    AddressSize = 4,
                    SegmentSelectorSize = 0,
                    Entries = []
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Equal((ushort)5, decoded.CompilationUnits[0].Version);
        Assert.Equal((byte)4, decoded.CompilationUnits[0].AddressSize);
        Assert.Empty(decoded.CompilationUnits[0].Entries);
    }

    #endregion

    #region ����Ŀ����

    [Fact]
    public void EncodeDecode_SingleEntryWithAttributes_Roundtrip()
    {
        var data = create_compilation_unit_with_attributes();

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Single(cu.Entries);

        var entry = cu.Entries[0];
        Assert.Equal(0x11u, entry.Tag);
        Assert.Equal(3, entry.Attributes.Count);

        var producer = entry.Attributes.FirstOrDefault(a => a.Name == 0x25u);
        Assert.NotNull(producer);
        Assert.Equal("MyCompiler 1.0", producer.Value as string);

        var language = entry.Attributes.FirstOrDefault(a => a.Name == 0x13u);
        Assert.NotNull(language);
        Assert.True(language.Value is byte and 2);

        var stmtList = entry.Attributes.FirstOrDefault(a => a.Name == 0x10u);
        Assert.NotNull(stmtList);
        Assert.True(stmtList.Value is uint and 0);
    }

    [Fact]
    public void EncodeDecode_MultipleEntries_Roundtrip()
    {
        var data = create_compilation_unit_with_multiple_entries();

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Equal(3, decoded.CompilationUnits[0].Entries.Count);

        Assert.Equal(0x11u, decoded.CompilationUnits[0].Entries[0].Tag);
        Assert.Equal(0x2Eu, decoded.CompilationUnits[0].Entries[1].Tag);
        Assert.Equal(0x34u, decoded.CompilationUnits[0].Entries[2].Tag);
    }

    #endregion

    #region �кű�����

    [Fact]
    public void EncodeDecode_LineNumberTable_Roundtrip()
    {
        var data = create_line_number_table();

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.LineNumberTables);
        var table = decoded.LineNumberTables[0];
        Assert.Equal((ushort)4, table.Version);
        Assert.Equal((byte)8, table.AddressSize);
        Assert.Equal((byte)0, table.SegmentSelectorSize);
        Assert.Equal((byte)1, table.MinimumInstructionLength);
        Assert.Equal((byte)1, table.MaximumOperationsPerInstruction);
        Assert.Equal((byte)1, table.DefaultIsStatement);
        Assert.Equal((sbyte)1, table.LineBase);
        Assert.Equal((byte)2, table.LineRange);
        Assert.Equal((byte)10, table.OpcodeBase);
        Assert.Equal(9, table.StandardOpcodeLengths.Count);
        Assert.Single(table.FileNames);
        Assert.Equal("main.c", table.FileNames[0]);
    }

    [Fact]
    public void EncodeDecode_LineTableWithMultipleFiles_Roundtrip()
    {
        var data = new DWARFFileData
        {
            CompilationUnits = [],
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 50,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = -5,
                    LineRange = 14,
                    OpcodeBase = 13,
                    StandardOpcodeLengths = [0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 0, 1],
                    FileNames = ["main.c", "helper.c", "common.h"]
                }
            ]
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.LineNumberTables);
        Assert.Equal(3, decoded.LineNumberTables[0].FileNames.Count);
    }

    #endregion

    #region �������

    [Fact]
    public void EncodeDecode_CUAndLineTable_Roundtrip()
    {
        var data = new DWARFFileData
        {
            CompilationUnits = [create_minimal_cu()],
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 40,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = 0,
                    LineRange = 1,
                    OpcodeBase = 5,
                    StandardOpcodeLengths = [0, 0, 0, 0],
                    FileNames = ["test.cpp"]
                }
            ]
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Single(decoded.LineNumberTables);
    }

    [Fact]
    public void EncodeDecode_MultipleCUs_Roundtrip()
    {
        var data = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
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
                                    Value = "cu1.c"
                                }
                            ]
                        }
                    ]
                },
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0x100,
                    AddressSize = 4,
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
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "cu2.c"
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(2, decoded.CompilationUnits.Count);
        Assert.Equal(0x11u, decoded.CompilationUnits[0].Entries[0].Tag);
        Assert.Equal(0x11u, decoded.CompilationUnits[1].Entries[0].Tag);
    }

    #endregion

    #region ������ʽ����

    [Theory]
    [InlineData(0x03, 42, "DW_FORM_addr")]
    [InlineData(0x05, true, "DW_FORM_flag")]
    [InlineData(0x06, 255, "DW_FORM_data1")]
    [InlineData(0x0A, 12345, "DW_FORM_data4")]
    [InlineData(0x0B, "hello", "DW_FORM_string")]
    [InlineData(0x0E, 123456, "DW_FORM_udata")]
    [InlineData(0x0F, 9876543210L, "DW_FORM_data8")]
    [InlineData(0x11, "world", "DW_FORM_strp")]
#pragma warning disable xUnit1026
    public void EncodeDecode_AttributeForm_Roundtrip(int form, object value, string formName)
#pragma warning restore xUnit1026
    {
        var data = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x34,
                            HasChildren = false,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x01,
                                    Form = (uint)form,
                                    Value = value
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Single(decoded.CompilationUnits[0].Entries);
        Assert.Single(decoded.CompilationUnits[0].Entries[0].Attributes);

        var attr = decoded.CompilationUnits[0].Entries[0].Attributes[0];
        Assert.Equal(0x01u, attr.Name);
        Assert.Equal((uint)form, attr.Form);

        if (value is string s)
        {
            Assert.Equal(s, attr.Value as string);
        }
        else if (value is bool b)
        {
            Assert.Equal(b, attr.Value);
        }
        else if (value is int i)
        {
            switch (attr.Value)
            {
                case int attrVal:
                    Assert.Equal(i, attrVal);
                    break;

                case uint attrValU:
                    Assert.Equal((uint)i, attrValU);
                    break;

                case ushort attrValUs:
                    Assert.Equal((ushort)i, attrValUs);
                    break;

                case byte attrValB:
                    Assert.Equal((byte)i, attrValB);
                    break;

                default:
                    Assert.Fail($"����ֵ���Ͳ�ƥ��: ���� int({i}), ʵ�� {attr.Value?.GetType().Name}({attr.Value})");
                    break;
            }
        }
        else if (value is long l)
        {
            Assert.Equal((ulong)l, attr.Value);
        }
    }

    #endregion

    #region ����Ŀ����

    [Fact]
    public void EncodeDecode_EntryWithChildren_Roundtrip()
    {
        var data = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x11,
                            HasChildren = true,
                            Attributes = []
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };

        var encoder = new DWARFEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new DWARFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Single(decoded.CompilationUnits);
        Assert.Single(decoded.CompilationUnits[0].Entries);
        Assert.True(decoded.CompilationUnits[0].Entries[0].HasChildren);
    }

    #endregion

    #region ��������

    private static DWARFCompilationUnitData create_minimal_cu()
    {
        return new DWARFCompilationUnitData
        {
            Version = 4,
            DebugInfoOffset = 0,
            AddressSize = 8,
            SegmentSelectorSize = 0,
            Entries = []
        };
    }

    private static DWARFFileData create_minimal_compilation_unit()
    {
        return new DWARFFileData
        {
            CompilationUnits = [create_minimal_cu()],
            LineNumberTables = []
        };
    }

    private static DWARFFileData create_compilation_unit_with_attributes()
    {
        return new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
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
                                    Name = 0x25,
                                    Form = 0x0B,
                                    Value = "MyCompiler 1.0"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x13,
                                    Form = 0x06,
                                    Value = 2
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x10,
                                    Form = 0x0E,
                                    Value = 0
                                }
                            ]
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };
    }

    private static DWARFFileData create_compilation_unit_with_multiple_entries()
    {
        return new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries =
                    [
                        new DWARFEntryData
                        {
                            AbbreviationCode = 1,
                            Tag = 0x11,
                            HasChildren = true,
                            Attributes = []
                        },
                        new DWARFEntryData
                        {
                            AbbreviationCode = 2,
                            Tag = 0x2E,
                            HasChildren = true,
                            Attributes =
                            [
                                new DWARFAttributeData
                                {
                                    Name = 0x03,
                                    Form = 0x0B,
                                    Value = "main"
                                }
                            ]
                        },
                        new DWARFEntryData
                        {
                            AbbreviationCode = 3,
                            Tag = 0x34,
                            HasChildren = false,
                            Attributes = []
                        }
                    ]
                }
            ],
            LineNumberTables = []
        };
    }

    private static DWARFFileData create_line_number_table()
    {
        return new DWARFFileData
        {
            CompilationUnits = [],
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 40,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = 1,
                    LineRange = 2,
                    OpcodeBase = 10,
                    StandardOpcodeLengths = [0, 0, 0, 0, 0, 0, 0, 0, 0],
                    FileNames = ["main.c"]
                }
            ]
        };
    }

    #endregion
}

