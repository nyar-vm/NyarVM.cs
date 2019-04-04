namespace Nyar.Tests.Binary;

public sealed class ZipRoundTripTests
{
    #region 基本往返测试

    [Fact]
    public void EncodeDecode_SingleFile_Roundtrip()
    {
        var content = "Hello ZIP World!"u8.ToArray();
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "hello.txt",
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal("hello.txt", result.Entries[0].Name);
        Assert.Equal(content.Length, result.Entries[0].Size);
        Assert.Equal(content, result.Entries[0].Data);
    }

    [Fact]
    public void EncodeDecode_MultipleFiles_Roundtrip()
    {
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "file1.txt",
                    Data = "Content 1"u8.ToArray(),
                    Size = 9
                },
                new()
                {
                    Name = "file2.txt",
                    Data = "Content 2"u8.ToArray(),
                    Size = 9
                },
                new()
                {
                    Name = "sub/file3.txt",
                    Data = "Content 3"u8.ToArray(),
                    Size = 9
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(3, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.Name == "file1.txt");
        Assert.Contains(result.Entries, e => e.Name == "file2.txt");
        Assert.Contains(result.Entries, e => e.Name == "sub/file3.txt");
    }

    [Fact]
    public void EncodeDecode_BinaryData_Roundtrip()
    {
        var binaryData = new byte[1024];
        for (var i = 0; i < binaryData.Length; i++)
        {
            binaryData[i] = (byte)(i % 256);
        }

        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "binary.bin",
                    Data = binaryData,
                    Size = binaryData.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal(binaryData.Length, result.Entries[0].Size);
        Assert.Equal(binaryData, result.Entries[0].Data);
    }

    [Fact]
    public void EncodeDecode_EmptyFile_Roundtrip()
    {
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "empty.txt",
                    Data = [],
                    Size = 0
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal("empty.txt", result.Entries[0].Name);
        Assert.Equal(0, result.Entries[0].Size);
        Assert.Empty(result.Entries[0].Data);
    }

    [Fact]
    public void EncodeDecode_EmptyArchive_Roundtrip()
    {
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>()
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Empty(result.Entries);
    }

    #endregion

    #region 压缩级别测试

    [Fact]
    public void EncodeDecode_NoCompression_Roundtrip()
    {
        var content = new byte[4096];
        new Random(42).NextBytes(content);

        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "stored.bin",
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder(System.IO.Compression.CompressionLevel.NoCompression);
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Equal(content, result.Entries[0].Data);
    }

    #endregion

    #region Scanner 验证

    [Fact]
    public void EncodeDecode_ScannerValidation()
    {
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "a.txt",
                    Data = "AAA"u8.ToArray(),
                    Size = 3
                },
                new()
                {
                    Name = "b.txt",
                    Data = "BBBB"u8.ToArray(),
                    Size = 4
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var scanner = new ZipScanner(bytes);
        Assert.True(scanner.ValidateHeader());

        var stats = scanner.ScanStatistics();
        Assert.Equal(2, stats.EntryCount);
        Assert.Contains(stats.EntryNames, n => n == "a.txt");
        Assert.Contains(stats.EntryNames, n => n == "b.txt");
    }

    #endregion

    #region 便捷方法测试

    [Fact]
    public void EncodeSingle_ProducesValidZip()
    {
        var data = "Single Entry"u8.ToArray();
        var encoder = new ZipEncoder();
        var bytes = encoder.EncodeSingle("single.txt", data);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal("single.txt", result.Entries[0].Name);
        Assert.Equal(data, result.Entries[0].Data);
    }

    [Fact]
    public void GetEntry_DecoderWorksWithEncodedData()
    {
        var content = "Find Me"u8.ToArray();
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "findme.txt",
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.GetEntry(bytes, "findme.txt");

        Assert.NotNull(result);
        Assert.Equal(content, result);
    }

    [Fact]
    public void GetEntryNames_DecoderWorksWithEncodedData()
    {
        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new() { Name = "1.txt", Data = "1"u8.ToArray(), Size = 1 },
                new() { Name = "2.txt", Data = "2"u8.ToArray(), Size = 1 },
                new() { Name = "3.txt", Data = "3"u8.ToArray(), Size = 1 }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var names = decoder.GetEntryNames(bytes);

        Assert.Equal(3, names.Count);
        Assert.Contains("1.txt", names);
        Assert.Contains("2.txt", names);
        Assert.Contains("3.txt", names);
    }

    #endregion

    #region 大文件测试

    [Fact]
    public void EncodeDecode_LargeFile_Roundtrip()
    {
        var content = new byte[100_000];
        for (var i = 0; i < content.Length; i++)
        {
            content[i] = (byte)(i % 251);
        }

        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = "large.dat",
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal(content.Length, result.Entries[0].Size);
        Assert.Equal(content, result.Entries[0].Data);
    }

    #endregion

    #region 名称测试

    [Fact]
    public void EncodeDecode_UnicodeNames_Roundtrip()
    {
        var unicodeName = "中文文件的txt";
        var content = "Unicode content"u8.ToArray();

        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = unicodeName,
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal(unicodeName, result.Entries[0].Name);
    }

    [Fact]
    public void EncodeDecode_DeepPath_Roundtrip()
    {
        var deepPath = "a/b/c/d/e/file.txt";
        var content = "deep"u8.ToArray();

        var original = new ZipFileData
        {
            Entries = new List<ZipEntryData>
            {
                new()
                {
                    Name = deepPath,
                    Data = content,
                    Size = content.Length
                }
            }
        };

        var encoder = new ZipEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ZipDecoder();
        var result = decoder.Decode(bytes);

        Assert.Single(result.Entries);
        Assert.Equal(deepPath, result.Entries[0].Name);
    }

    #endregion
}
