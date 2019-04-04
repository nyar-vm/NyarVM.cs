using Nyar.Binary.MachO.Data;
using Nyar.Binary.MachO.Encode;

using System.Text;

namespace Nyar.Tests.Binary;

public sealed class MachOEncoderTests
{
    // -region-
    [Fact]
    public void Encode_Minimal64Bit_ProducesValidMachO()
    {
        var data = create_minimal_mach_o_data(true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 32);
        Assert.Equal(0xCF, bytes[0]);
        Assert.Equal(0xFA, bytes[1]);
        Assert.Equal(0xED, bytes[2]);
        Assert.Equal(0xFE, bytes[3]);
    }

    [Fact]
    public void Encode_Minimal32Bit_ProducesValidMachO()
    {
        var data = create_minimal_mach_o_data(false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 28);
        Assert.Equal(0xCE, bytes[0]);
        Assert.Equal(0xFA, bytes[1]);
        Assert.Equal(0xED, bytes[2]);
        Assert.Equal(0xFE, bytes[3]);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_64BitMagic_IsFEEDFACF()
    {
        var data = create_minimal_mach_o_data(true);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var magic = read_u32_le(bytes, 0);
        Assert.Equal(0xFEEDFACFu, magic);
    }

    [Fact]
    public void Encode_32BitMagic_IsFEEDFACE()
    {
        var data = create_minimal_mach_o_data(false);
        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var magic = read_u32_le(bytes, 0);
        Assert.Equal(0xFEEDFACEu, magic);
    }

    [Fact]
    public void Encode_CpuType_PreservesValue()
    {
        var data = create_minimal_mach_o_data(true, 0x01000007);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var cpuType = read_i32_le(bytes, 4);
        Assert.Equal(0x01000007, cpuType);
    }

    [Fact]
    public void Encode_FileType_PreservesValue()
    {
        var data = create_minimal_mach_o_data(true, fileType: 2);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var fileType = read_u32_le(bytes, 12);
        Assert.Equal(2u, fileType);
    }

    [Fact]
    public void Encode_Flags_PreservesValue()
    {
        var data = create_minimal_mach_o_data(true, flags: 0x200085);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var flags = read_u32_le(bytes, 24);
        Assert.Equal(0x200085u, flags);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_WithSectionContent_IncludesContentBytes()
    {
        var textContent = new byte[] { 0x55, 0x48, 0x89, 0xE5, 0xC3 };
        var data = create_mach_o_with_section_content(textContent);

        var encoder = new MachOEncoder();
        var bytes = encoder.Encode(data);

        var found = false;
        for (var i = 0; i <= bytes.Length - textContent.Length; i++)
        {
            var match = true;
            for (var j = 0; j < textContent.Length; j++)
            {
                if (bytes[i + j] != textContent[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                found = true;
                break;
            }
        }

        Assert.True(found, "节区内容未在");
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
                    SectionName = "__text",
                    SegmentName = "__TEXT",
                    Address = 0,
                    Size = (ulong)textContent.Length,
                    Offset = (uint)textOffset,
                    Alignment = 4,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0x80000400,
                    Content = textContent
                }
            ]
        };
    }

    private static MachOFileData create_mach_o_with_multiple_sections(byte[] textContent, byte[] dataContent)
    {
        var headerSize = 32;
        var segmentCommandSize = 72 + 80 * 2;
        var textOffset = headerSize + segmentCommandSize;
        var dataOffset = textOffset + textContent.Length;
        dataOffset = (dataOffset + 15) & ~15;

        var segmentData = new byte[segmentCommandSize - 8];
        var nameBytes = Encoding.UTF8.GetBytes("__TEXT\0\0\0\0\0\0\0\0\0\0");
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
                    SectionName = "__text",
                    SegmentName = "__TEXT",
                    Address = 0,
                    Size = (ulong)textContent.Length,
                    Offset = (uint)textOffset,
                    Alignment = 4,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0x80000400,
                    Content = textContent
                },
                new MachOSectionData
                {
                    SectionName = "__data",
                    SegmentName = "__DATA",
                    Address = (ulong)textContent.Length,
                    Size = (ulong)dataContent.Length,
                    Offset = (uint)dataOffset,
                    Alignment = 3,
                    RelocationsOffset = 0,
                    NumberOfRelocations = 0,
                    Flags = 0,
                    Content = dataContent
                }
            ]
        };
    }

    // [/section renamed - encoding fix]
}


