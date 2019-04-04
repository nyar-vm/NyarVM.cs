using Nyar.Binary.Dwarf.Data;
using Nyar.Binary.Dwarf.Encode;
using Nyar.Binary.Dwarf.Decode;

namespace Nyar.Tests.Binary;

/// <summary>
/// DWARF �������������Ե�
///</summary>
public class DwarfEncoderTests
{
    /// <summary>
    /// �������ԣ���DWARF �ļ����ޱ��뵥Ԫ�����кű�����    
///</summary>
    [Fact]
    public void RoundTrip_EmptyFile()
    {
        var original = new DWARFFileData
        {
            CompilationUnits = [],
            LineNumberTables = []
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Empty(decoded.CompilationUnits);
        Assert.Empty(decoded.LineNumberTables);
    }

    /// <summary>
    /// �������ԣ���С���뵥Ԫ������Ŀ����    
///</summary>
    [Fact]
    public void RoundTrip_MinimalCompilationUnit()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries = []
                }
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal((ushort)4, cu.Version);
        Assert.Equal((uint)0, cu.DebugInfoOffset);
        Assert.Equal((byte)8, cu.AddressSize);
        Assert.Equal((byte)0, cu.SegmentSelectorSize);
        Assert.Empty(cu.Entries);
    }

    /// <summary>
    /// �������ԣ���������Ŀ�ı��뵥Ԫ��    
///</summary>
    [Fact]
    public void RoundTrip_SingleEntry()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
                    Version = 5,
                    DebugInfoOffset = 42,
                    AddressSize = 4,
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
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal((ushort)5, cu.Version);
        Assert.Equal((uint)42, cu.DebugInfoOffset);
        Assert.Equal((byte)4, cu.AddressSize);
        Assert.Equal((byte)0, cu.SegmentSelectorSize);
        Assert.Single(cu.Entries);

        var entry = cu.Entries[0];
        Assert.Equal((ulong)1, entry.AbbreviationCode);
        Assert.Equal((uint)0x11, entry.Tag);
        Assert.True(entry.HasChildren);
        Assert.Empty(entry.Attributes);
    }

    /// <summary>
    /// �������ԣ�����������Ŀ�ı��뵥Ԫ��    
///</summary>
    [Fact]
    public void RoundTrip_MultipleEntries()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
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
                                    Form = 0x01,
                                    Value = null
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
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal((ushort)4, cu.Version);
        Assert.Equal(3, cu.Entries.Count);

        var entry1 = cu.Entries[0];
        Assert.Equal((ulong)1, entry1.AbbreviationCode);
        Assert.Equal((uint)0x11, entry1.Tag);
        Assert.True(entry1.HasChildren);
        Assert.Single(entry1.Attributes);
        Assert.Equal((uint)0x03, entry1.Attributes[0].Name);
        Assert.Equal((uint)0x01, entry1.Attributes[0].Form);
        Assert.Null(entry1.Attributes[0].Value);

        var entry2 = cu.Entries[1];
        Assert.Equal((ulong)2, entry2.AbbreviationCode);
        Assert.Equal((uint)0x2E, entry2.Tag);
        Assert.False(entry2.HasChildren);
        Assert.Single(entry2.Attributes);
        Assert.Equal((uint)0x03, entry2.Attributes[0].Name);
        Assert.Equal((uint)0x0B, entry2.Attributes[0].Form);
        Assert.Equal("main", entry2.Attributes[0].Value);

        var entry3 = cu.Entries[2];
        Assert.Equal((ulong)3, entry3.AbbreviationCode);
        Assert.Equal((uint)0x34, entry3.Tag);
        Assert.False(entry3.HasChildren);
        Assert.Empty(entry3.Attributes);
    }

    /// <summary>
    /// �������ԣ���������ʽ��ֵ����������    
    ///     ��0x01��null���������εġ�����Ϊ decoder Ҳ�������Ƶ�    
///</summary>
    [Fact]
    public void RoundTrip_AllAttributeForms()
    {
        var testCases = new (uint Form, object? Value, Type ExpectedType)[]
        {
            (0x01, null, null!),
            (0x03, (ushort)100, typeof(ushort)),
            (0x04, (uint)200, typeof(uint)),
            (0x05, true, typeof(bool)),
            (0x06, (byte)6, typeof(byte)),
            (0x07, (byte)7, typeof(byte)),
            (0x08, (ulong)8, typeof(ulong)),
            (0x09, (ushort)9, typeof(ushort)),
            (0x0A, (uint)10, typeof(uint)),
            (0x0B, "hello", typeof(string)),
            (0x0C, (byte)12, typeof(byte)),
            (0x0D, (ushort)13, typeof(ushort)),
            (0x0E, (uint)14, typeof(uint)),
            (0x0F, (ulong)15, typeof(ulong)),
            (0x10, (ulong)16, typeof(ulong)),
            (0x11, "world", typeof(string))
        };

        foreach (var tc in testCases)
        {
            var original = new DWARFFileData
            {
                CompilationUnits =
                [
                    new DWARFCompilationUnitData
                    {
                        UnitLength = 0,
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
                                HasChildren = false,
                                Attributes =
                                [
                                    new DWARFAttributeData
                                    {
                                        Name = 0x03,
                                        Form = tc.Form,
                                        Value = tc.Value
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            var encoded = encode(original);
            var decoded = decode(encoded);

            Assert.Single(decoded.CompilationUnits);
            var cu = decoded.CompilationUnits[0];
            Assert.Single(cu.Entries);
            var entry = cu.Entries[0];
            Assert.Single(entry.Attributes);

            var attr = entry.Attributes[0];
            Assert.Equal((uint)0x03, attr.Name);
            Assert.Equal(tc.Form, attr.Form);

            if (tc.Form == 0x01)
            {
                Assert.Null(attr.Value);
            }
            else
            {
                Assert.NotNull(attr.Value);
                Assert.IsType(tc.ExpectedType, attr.Value);
            }
        }
    }

    /// <summary>
    /// �������ԣ�������+ �����ԣ�����������ʽ��    
///</summary>
    [Fact]
    public void RoundTrip_MultipleEntriesWithAllForms()
    {
        var entries = new List<DWARFEntryData>();

        uint formIndex = 3;
        for (uint code = 1; code <= 15; code++)
        {
            object? value = formIndex switch
            {
                3 => (ushort)(100 + code),
                4 => (uint)(200 + code),
                5 => code % 2 == 0,
                6 => (byte)(code),
                7 => (byte)(code),
                8 => (ulong)(300 + code),
                9 => (ushort)(code),
                10 => (uint)(400 + code),
                11 => $"str_{code}",
                12 => (byte)(code),
                13 => (ushort)(500 + code),
                14 => (uint)(600 + code),
                15 => (ulong)(700 + code),
                16 => (ulong)(800 + code),
                17 => $"nstr_{code}",
                _ => null
            };

            entries.Add(new DWARFEntryData
            {
                AbbreviationCode = code,
                Tag = 0x11,
                HasChildren = code % 3 == 0,
                Attributes =
                [
                    new DWARFAttributeData
                    {
                        Name = 0x2F + code,
                        Form = formIndex,
                        Value = value
                    }
                ]
            });

            formIndex++;
            if (formIndex > 17)
            {
                formIndex = 3;
            }
        }

        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
                    Version = 4,
                    DebugInfoOffset = 0,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries = entries
                }
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.CompilationUnits);
        var cu = decoded.CompilationUnits[0];
        Assert.Equal(15, cu.Entries.Count);

        for (var i = 0; i < entries.Count; i++)
        {
            Assert.Equal(entries[i].AbbreviationCode, cu.Entries[i].AbbreviationCode);
            Assert.Equal(entries[i].Tag, cu.Entries[i].Tag);
            Assert.Equal(entries[i].HasChildren, cu.Entries[i].HasChildren);
            Assert.Single(cu.Entries[i].Attributes);
        }
    }

    /// <summary>
    /// �������ԣ�������뵥Ԫ��    
///</summary>
    [Fact]
    public void RoundTrip_MultipleCompilationUnits()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
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
                                    Value = "cu1"
                                }
                            ]
                        }
                    ]
                },
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
                    Version = 5,
                    DebugInfoOffset = 100,
                    AddressSize = 4,
                    SegmentSelectorSize = 1,
                    Entries =
                    [
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
                                    Value = "func"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x04,
                                    Form = 0x04,
                                    Value = (uint)1024
                                }
                            ]
                        }
                    ]
                },
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
                    Version = 4,
                    DebugInfoOffset = 200,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    Entries = []
                }
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Equal(3, decoded.CompilationUnits.Count);

        Assert.Equal((ushort)4, decoded.CompilationUnits[0].Version);
        Assert.Equal((ushort)5, decoded.CompilationUnits[1].Version);
        Assert.Equal((ushort)4, decoded.CompilationUnits[2].Version);

        Assert.Equal((uint)0, decoded.CompilationUnits[0].DebugInfoOffset);
        Assert.Equal((uint)100, decoded.CompilationUnits[1].DebugInfoOffset);
        Assert.Equal((uint)200, decoded.CompilationUnits[2].DebugInfoOffset);

        Assert.Equal((byte)8, decoded.CompilationUnits[0].AddressSize);
        Assert.Equal((byte)4, decoded.CompilationUnits[1].AddressSize);
        Assert.Equal((byte)8, decoded.CompilationUnits[2].AddressSize);

        Assert.Single(decoded.CompilationUnits[0].Entries);
        Assert.Single(decoded.CompilationUnits[1].Entries);
        Assert.Empty(decoded.CompilationUnits[2].Entries);

        Assert.Equal(2, decoded.CompilationUnits[1].Entries[0].Attributes.Count);
        Assert.Equal((uint)1024, decoded.CompilationUnits[1].Entries[0].Attributes[1].Value);
    }

    /// <summary>
    /// �������ԣ�������ٴα��룬��֤�ݵ��Ե�    
///</summary>
    [Fact]
    public void RoundTrip_Idempotent()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
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
                                    Value = "test"
                                },
                                new DWARFAttributeData
                                {
                                    Name = 0x04,
                                    Form = 0x04,
                                    Value = (uint)42
                                }
                            ]
                        },
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
                                    Value = "var"
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        var pass1Encoded = encode(original);
        var pass1Decoded = decode(pass1Encoded);
        var pass2Encoded = encode(pass1Decoded);
        var pass2Decoded = decode(pass2Encoded);

        Assert.Equal(pass1Encoded.Length, pass2Encoded.Length);
        Assert.True(pass1Encoded.AsSpan().SequenceEqual(pass2Encoded));

        Assert.Equal(pass1Decoded.CompilationUnits.Count, pass2Decoded.CompilationUnits.Count);
        Assert.Equal(pass1Decoded.CompilationUnits[0].Entries.Count, pass2Decoded.CompilationUnits[0].Entries.Count);
    }

    /// <summary>
    /// �������ԣ��кű������    
///</summary>
    [Fact]
    public void RoundTrip_LineNumberTable()
    {
        var original = new DWARFFileData
        {
            CompilationUnits = [],
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    UnitLength = 0,
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 50,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = -5,
                    LineRange = 14,
                    OpcodeBase = 10,
                    StandardOpcodeLengths = [1, 1, 1, 1, 0, 0, 0, 1, 0],
                    FileNames = ["file1.c", "file2.h"]
                }
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.LineNumberTables);
        var table = decoded.LineNumberTables[0];
        Assert.Equal((ushort)4, table.Version);
        Assert.Equal((byte)8, table.AddressSize);
        Assert.Equal((byte)0, table.SegmentSelectorSize);
        Assert.Equal((uint)50, table.HeaderLength);
        Assert.Equal((byte)1, table.MinimumInstructionLength);
        Assert.Equal((byte)1, table.MaximumOperationsPerInstruction);
        Assert.Equal((byte)1, table.DefaultIsStatement);
        Assert.Equal((sbyte)(-5), table.LineBase);
        Assert.Equal((byte)14, table.LineRange);
        Assert.Equal((byte)10, table.OpcodeBase);
        Assert.Equal(9, table.StandardOpcodeLengths.Count);
        Assert.Equal([1, 1, 1, 1, 0, 0, 0, 1, 0], table.StandardOpcodeLengths);
        Assert.Equal(2, table.FileNames.Count);
        Assert.Equal("file1.c", table.FileNames[0]);
        Assert.Equal("file2.h", table.FileNames[1]);
    }

    /// <summary>
    /// �������ԣ����뵥Ԫ + �кű���ϵ�    
///</summary>
    [Fact]
    public void RoundTrip_CompilationUnitAndLineTable()
    {
        var original = new DWARFFileData
        {
            CompilationUnits =
            [
                new DWARFCompilationUnitData
                {
                    UnitLength = 0,
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
            LineNumberTables =
            [
                new DWARFLineNumberTableData
                {
                    UnitLength = 0,
                    Version = 4,
                    AddressSize = 8,
                    SegmentSelectorSize = 0,
                    HeaderLength = 30,
                    MinimumInstructionLength = 1,
                    MaximumOperationsPerInstruction = 1,
                    DefaultIsStatement = 1,
                    LineBase = -3,
                    LineRange = 12,
                    OpcodeBase = 5,
                    StandardOpcodeLengths = [1, 1, 1, 0],
                    FileNames = ["test.c"]
                }
            ]
        };

        var encoded = encode(original);
        var decoded = decode(encoded);

        Assert.Single(decoded.CompilationUnits);
        Assert.Single(decoded.LineNumberTables);

        Assert.Single(decoded.CompilationUnits[0].Entries);
        Assert.Equal("test.c", decoded.LineNumberTables[0].FileNames[0]);
    }

    private static byte[] encode(DWARFFileData data)
    {
        var encoder = new DWARFEncoder();
        return encoder.Encode(data);
    }

    private static DWARFFileData decode(byte[] data)
    {
        var decoder = new DWARFDecoder();
        return decoder.Decode(data);
    }
}

