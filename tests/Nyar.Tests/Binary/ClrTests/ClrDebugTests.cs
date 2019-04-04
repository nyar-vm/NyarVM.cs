namespace Nyar.Tests.Binary.ClrTests;

public class ClrDebugTests
{
    [Fact]
    public void Debug_DumpPeBytes()
    {
        var module = new ClrModuleData
        {
            module_name = "Test",
            version = "v4.0.30319",
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(module);

        Assert.True(bytes.Length > 64, $"PE file too short: {bytes.Length} bytes");

        var dosMagic = BitConverter.ToUInt16(bytes, 0);
        Assert.Equal((ushort)0x5A4D, dosMagic);

        var peOffset = BitConverter.ToInt32(bytes, 60);
        Assert.True(peOffset > 0 && peOffset < bytes.Length, $"PE offset out of range: {peOffset}");

        var peMagic = BitConverter.ToUInt32(bytes, peOffset);
        Assert.Equal((uint)0x00004550, peMagic);
    }

    [Fact]
    public void Debug_PeDecoder_Direct()
    {
        var module = new ClrModuleData
        {
            module_name = "Test",
            version = "v4.0.30319",
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(module);

        var peDecoder = new PeDecoder();
        var peFile = peDecoder.Decode(bytes);

        Assert.NotNull(peFile);
        Assert.Equal((ushort)0x014C, peFile.Header.Machine);
        Assert.True(peFile.Header.NumberOfSections >= 1);
    }

    [Fact]
    public void Debug_ClrDirectory_Bytes()
    {
        var module = new ClrModuleData
        {
            module_name = "Test",
            version = "v4.0.30319",
            methods = [],
            types = [],
            fields = [],
            properties = [],
            events = []
        };

        var encoder = new ClrEncoder();
        var bytes = encoder.encode(module);

        var peDecoder = new PeDecoder();
        var peFile = peDecoder.Decode(bytes);

        var clrDir = peFile.GetDataDirectory(PeDirectoryDataIndex.ClrRuntimeHeader);
        Assert.False(clrDir.IsEmpty, "CLR 目录数据目录为空");

        var clrOffset = peFile.RvaToOffset(clrDir.Rva);
        Assert.True(clrOffset > 0, $"CLR 目录偏移无效: {clrOffset}");

        var cb = BitConverter.ToUInt32(bytes, clrOffset);
        Assert.Equal((uint)72, cb);

        var metadataRva = BitConverter.ToUInt32(bytes, clrOffset + 8);
        Assert.True(metadataRva > 0, $"MetadataRva 的0, 实际的 0x{metadataRva:X8}");

        var metadataOffset = peFile.RvaToOffset(metadataRva);
        Assert.True(metadataOffset > 0, $"元数据偏移无的 {metadataOffset}");

        Assert.True(metadataOffset < bytes.Length, $"元数据偏移超出文的 {metadataOffset} >= {bytes.Length}");

        var metadataSig = BitConverter.ToUInt32(bytes, metadataOffset);
        if (metadataSig != 0x424A5342)
        {
            var hexDump = new System.Text.StringBuilder();
            hexDump.AppendLine($"文件大小: {bytes.Length}");
            hexDump.AppendLine($"CLR 目录偏移: 0x{clrOffset:X}");
            hexDump.AppendLine($"MetadataRva: 0x{metadataRva:X}");
            hexDump.AppendLine($"元数据偏的 0x{metadataOffset:X}");
            hexDump.AppendLine($"元数据签的 0x{metadataSig:X8}");
            hexDump.AppendLine($"0x2C8 处字的 0x{BitConverter.ToUInt32(bytes, 0x2C8):X8}");
            hexDump.AppendLine($"0x200 处字的 0x{BitConverter.ToUInt32(bytes, 0x200):X8}");
            hexDump.AppendLine($"0x280 处字的 0x{BitConverter.ToUInt32(bytes, 0x280):X8}");

            for (var i = 0x200; i < Math.Min(0x300, bytes.Length); i += 16)
            {
                hexDump.Append($"0x{i:X4}: ");
                for (var j = 0; j < 16 && i + j < bytes.Length; j++)
                {
                    hexDump.Append($"{bytes[i + j]:X2} ");
                }

                hexDump.AppendLine();
            }

            Assert.Fail($"元数据签名不匹配\n{hexDump}");
        }

        Assert.Equal((uint)0x424A5342, metadataSig);
    }
}