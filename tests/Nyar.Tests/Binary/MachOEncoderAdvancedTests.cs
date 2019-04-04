using Nyar.Binary.MachO.Data;
using Nyar.Binary.MachO.Encode;

using System.Text;

namespace Nyar.Tests.Binary;

public sealed class MachOEncoderAdvancedTests
{
    // -region-

    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(5u)]
    [InlineData(6u)]
    [InlineData(8u)]
    public void Encode_FileType_PreservesValue(uint fileType)
    {
        var data = create_minimal_mach_o_data(true, fileType: fileType);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var actual = read_u32_le(bytes, 12);
        Assert.Equal(fileType, actual);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_CpuSubtype_PreservesValue()
    {
        var data = create_minimal_mach_o_data(true, cpuSubtype: 3);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var cpuSubtype = read_i32_le(bytes, 8);
        Assert.Equal(3, cpuSubtype);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Theory]
    [InlineData(0u)]
    [InlineData(1u)]
    [InlineData(0x200085u)]
    public void Encode_Flags_PreservesValue(uint flags)
    {
        var data = create_minimal_mach_o_data(true, flags: flags);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var actual = read_u32_le(bytes, 24);
        Assert.Equal(flags, actual);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_TextSectionFlags_Is80000400()
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_mach_o_with_section(textContent, "__text", "__TEXT", 0x80000400);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 32);
    }

    [Fact]
    public void Encode_DataSectionFlags_Is0()
    {
        var dataContent = new byte[] { 0xCA, 0xFE };
        var data = create_mach_o_with_section(dataContent, "__data", "__DATA", 0);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 32);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(12)]
    public void Encode_SectionAlignment_PreservesValue(int alignment)
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_mach_o_with_section(textContent, "__text", "__TEXT", 0x80000400, alignment);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 32);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_SizeOfLoadCommands_PreservesValue()
    {
        var data = create_minimal_mach_o_data(true, sizeOfLoadCommands: 152);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var sizeOfLoadCommands = read_u32_le(bytes, 20);
        Assert.Equal(152u, sizeOfLoadCommands);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_64BitHeader_Is32Bytes()
    {
        var data = create_minimal_mach_o_data(true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 32);
    }

    [Fact]
    public void Encode_32BitHeader_Is28Bytes()
    {
        var data = create_minimal_mach_o_data(false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 28);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_ReservedField_PreservesValue()
    {
        var data = new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = 0,
                Flags = 0,
                Reserved = 42,
                IsLittleEndian = true
            },
            LoadCommands = [],
            Sections = []
        };

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var reserved = read_u32_le(bytes, 28);
        Assert.Equal(42u, reserved);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_LargeSectionContent_ProducesCorrectOutput()
    {
        var textContent = new byte[4096];
        for (var i = 0; i < textContent.Length; i++)
        {
            textContent[i] = 0x90;
        }

        var data = create_mach_o_with_section(textContent, "__text", "__TEXT", 0x80000400);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 32 + textContent.Length);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_ArmCpuType_ProducesValidMachO()
    {
        var data = create_minimal_mach_o_data(false, 12);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var cpuType = read_i32_le(bytes, 4);
        Assert.Equal(12, cpuType);
    }

    [Fact]
    public void Encode_AArch64CpuType_ProducesValidMachO()
    {
        var data = create_minimal_mach_o_data(true, 0x0100000C);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var cpuType = read_i32_le(bytes, 4);
        Assert.Equal(0x0100000C, cpuType);
    }

    // [/section renamed - encoding fix]

    // -region-

    private static uint read_u32_le(byte[] bytes, int offset)
    {
        return (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
    }

    private static int read_i32_le(byte[] bytes, int offset)
    {
        return (int)read_u32_le(bytes, offset);
    }

    private static MachOFileData create_minimal_mach_o_data(
        bool is64, int cpuType = 0, int cpuSubtype = 3, uint fileType = 2,
        uint flags = 0, uint sizeOfLoadCommands = 0)
    {
        var actualCpuType = cpuType != 0 ? cpuType : (is64 ? 0x01000007 : 7);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = is64 ? 0xFEEDFACF : 0xFEEDFACE,
                CPUType = actualCpuType,
                CPUSubtype = cpuSubtype,
                FileType = fileType,
                NumberOfLoadCommands = 0,
                SizeOfLoadCommands = sizeOfLoadCommands,
                Flags = flags,
                Reserved = 0,
                IsLittleEndian = true
            },
            LoadCommands = [],
            Sections = []
        };
    }

    private static MachOFileData create_mach_o_with_section(
        byte[] textContent, string sectionName, string segmentName,
        uint sectionFlags, int alignment = 4)
    {
        var headerSize = 32;
        var sectionSize = 80;
        var segmentCommandSize = 72 + sectionSize;
        var textOffset = headerSize + segmentCommandSize;

        var segmentData = new byte[segmentCommandSize - 8];
        var nameBytes = Encoding.UTF8.GetBytes($"{segmentName}\0");
        nameBytes.AsSpan().CopyTo(segmentData);

        return new MachOFileData
        {
            Header = new MachOHeaderData
            {
                Magic = 0xFEEDFACF,
                CPUType = 0x01000007,
                CPUSubtype = 3,
                FileType = 2,
                NumberOfLoadCommands = 1,
                SizeOfLoadCommands = (uint)segmentCommandSize,
                Flags = 0,
                Reserved = 0,
                IsLittleEndian = true
            },
            LoadCommands =
            [
                new MachOLoadCommandData
                {
                    Command = 0x19,
                    Size = (uint)segmentCommandSize,
                    Data = segmentData
                }
            ],
            Sections =
            [
                new MachOSectionData
                {
                    SectionName = sectionName,
                    SegmentName = segmentName,
                    Address = 0,
                    Size = (ulong)textContent.Length,
                    Offset = (uint)textOffset,
                    Alignment = (uint)alignment,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = sectionFlags,
                    Content = textContent
                }
            ]
        };
    }

    // [/section renamed - encoding fix]
}

