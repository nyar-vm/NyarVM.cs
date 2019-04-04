namespace Nyar.Tests.Binary;

public sealed class FlatBuffersRoundTripTests
{
    #region ������������

    [Fact]
    public void EncodeDecode_SingleBoolField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Bool,
                        Value = true
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
        Assert.Equal(0, table.fields[0].Index);
        Assert.Equal(4, table.fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleByteField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Byte,
                        Value = (sbyte)42
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
        Assert.Equal(4, table.fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleIntField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 12345
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
        Assert.Equal(0, table.fields[0].Index);
        Assert.Equal(4, table.fields[0].VTableOffset);
    }

    [Fact]
    public void EncodeDecode_SingleFloatField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Float,
                        Value = 3.14f
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_SingleDoubleField_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Double,
                        Value = 2.718281828
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_MultipleFields_Roundtrip()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 100
                    },
                    new()
                    {
                        Index = 1,
                        VTableOffset = 8,
                        Type = FlatBufferFieldType.Float,
                        Value = 1.5f
                    },
                    new()
                    {
                        Index = 2,
                        VTableOffset = 12,
                        Type = FlatBufferFieldType.Bool,
                        Value = true
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Equal(3, table.fields.Count);
    }

    #endregion

    #region �ļ���ʶ������

    [Fact]
    public void EncodeDecode_WithFileIdentifier_Roundtrip()
    {
        var data = new FlatBufferData
        {
            FileIdentifier = "TEST",
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 42
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var fileId = decoder.ReadFileIdentifier();

        Assert.Equal("TEST", fileId);

        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    #endregion

    #region Scanner ��֤

    [Fact]
    public void EncodeDecode_ScannerValidation()
    {
        var data = new FlatBufferData
        {
            FileIdentifier = "SCAN",
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Int,
                        Value = 99
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var scanner = new FlatBuffersScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.Equal("SCAN", header.FileIdentifier);
        Assert.True(header.RootOffset > 0);
    }

    [Fact]
    public void EncodeDecode_WithoutFileId_ScannerValidation()
    {
        var data = new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = FlatBufferFieldType.Short,
                        Value = (short)7
                    }
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var scanner = new FlatBuffersScanner(bytes);
        var header = scanner.ScanHeader();

        Assert.True(header.RootOffset > 0);
    }

    #endregion

    #region ���б������Ͳ���

    [Fact]
    public void EncodeDecode_UByte_Roundtrip()
    {
        var data = create_single_field_data(FlatBufferFieldType.UByte, (byte)200);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_UShort_Roundtrip()
    {
        var data = create_single_field_data(FlatBufferFieldType.UShort, (ushort)50000);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_UInt_Roundtrip()
    {
        var data = create_single_field_data(FlatBufferFieldType.UInt, 0xFFFFFFFFu);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_Long_Roundtrip()
    {
        var data = create_single_field_data(FlatBufferFieldType.Long, 0x7FFFFFFFFFFFFFFF);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    [Fact]
    public void EncodeDecode_ULong_Roundtrip()
    {
        var data = create_single_field_data(FlatBufferFieldType.ULong, 0xFFFFFFFFFFFFFFFFu);
        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.Encode(data);

        var decoder = new FlatBuffersDecoder(bytes);
        var rootOffset = decoder.DecodeRootOffset();
        var table = decoder.DecodeTable(rootOffset);

        Assert.Single(table.fields);
    }

    #endregion

    #region ��ݷ�������

    [Fact]
    public void EncodeTable_Directly_Roundtrip()
    {
        var table = new FlatBufferTable
        {
            Fields = new List<FlatBufferField>
            {
                new()
                {
                    Index = 0,
                    VTableOffset = 4,
                    Type = FlatBufferFieldType.Int,
                    Value = 256
                }
            }
        };

        var encoder = new FlatBuffersEncoder();
        var bytes = encoder.EncodeTable(table, "DIRT");

        var decoder = new FlatBuffersDecoder(bytes);
        var fileId = decoder.ReadFileIdentifier();

        Assert.Equal("DIRT", fileId);

        var rootOffset = decoder.DecodeRootOffset();
        var decoded = decoder.DecodeTable(rootOffset);

        Assert.Single(decoded.fields);
    }

    #endregion

    private static FlatBufferData create_single_field_data(FlatBufferFieldType type, object value)
    {
        return new FlatBufferData
        {
            RootTable = new FlatBufferTable
            {
                Fields = new List<FlatBufferField>
                {
                    new()
                    {
                        Index = 0,
                        VTableOffset = 4,
                        Type = type,
                        Value = value
                    }
                }
            }
        };
    }
}
