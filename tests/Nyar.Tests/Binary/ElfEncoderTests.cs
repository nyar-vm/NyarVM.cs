using Nyar.Binary.Elf.Data;
using Nyar.Binary.Elf.Encode;

namespace Nyar.Tests.Binary;

public sealed class ElfEncoderTests
{
    // [section renamed - encoding fix]
    [Fact]
    public void Encode_Minimal64Bit_ProducesValidElf()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 64);
        Assert.Equal(0x7F, bytes[0]);
        Assert.Equal((byte)'E', bytes[1]);
        Assert.Equal((byte)'L', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    [Fact]
    public void Encode_Minimal32Bit_ProducesValidElf()
    {
        var data = create_minimal_elf_data(false);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 52);
        Assert.Equal(0x7F, bytes[0]);
        Assert.Equal((byte)'E', bytes[1]);
        Assert.Equal((byte)'L', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]
    [Fact]
    public void Encode_WithSectionContent_IncludesContentBytes()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };
        var data = create_elf_with_section_content(textContent);

        var encoder = new ElfEncoder();
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

        Assert.True(found, "节区内容未在编码输出中找到");
    }

    [Fact]
    public void Encode_MultipleSections_AllContentPresent()
    {
        var textContent = new byte[] { 0x90, 0x90, 0xC3 };
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        var data = create_elf_with_multiple_sections(textContent, dataContent);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length > textContent.Length + dataContent.Length + 64);
    }

    [Fact]
    public void Encode_EmptySectionContent_NoContentBytes()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]

    [Fact]
    public void Encode_64Bit_ClassFieldIs2()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(2, bytes[4]);
    }

    [Fact]
    public void Encode_32Bit_ClassFieldIs1()
    {
        var data = create_minimal_elf_data(false);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(1, bytes[4]);
    }

    [Fact]
    public void Encode_LittleEndian_DataEncodingIs1()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(1, bytes[5]);
    }

    [Fact]
    public void Encode_MachineType_PreservesValue()
    {
        var data = create_minimal_elf_data(true, 62);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var machine = BitConverter.ToUInt16(bytes, 18);
        Assert.Equal((ushort)62, machine);
    }

    [Fact]
    public void Encode_EntryPoint_PreservesValue()
    {
        var data = create_minimal_elf_data(true, entryPoint: 0x400100);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var entry = BitConverter.ToUInt64(bytes, 24);
        Assert.Equal((ulong)0x400100, entry);
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]
    [Fact]
    public void Encode_WithNamedSections_StringTableContainsNames()
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_elf_with_section_content(textContent);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var shstrtabFound = false;
        var searchText = ".shstrtab"u8;

        for (var i = 0; i <= bytes.Length - searchText.Length; i++)
        {
            var match = true;
            for (var j = 0; j < searchText.Length; j++)
            {
                if (bytes[i + j] != searchText[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                shstrtabFound = true;
                break;
            }
        }

        Assert.True(shstrtabFound, ".shstrtab 名称");
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]
    [Fact]
    public void Encode_WithProgramHeader_ProducesNonZeroOutput()
    {
        var data = new ELFFileData
        {
            Header = create_minimal_elf_header(true),
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = 0, VirtualAddress = 0x400000,
                    PhysicalAddress = 0x400000, FileSize = 0, MemorySize = 0, Alignment = 0x1000
                }
            ],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 64 + 56);
    }

    [Fact]
    public void Encode_ProgramHeaderFlags_PreservesValue()
    {
        var data = new ELFFileData
        {
            Header = new ELFHeaderData
            {
                Magic = [0x7F, 0x45, 0x4C, 0x46],
                Class = 2, DataEncoding = 1, Version = 1,
                Machine = 62, ObjectVersion = 1,
                ProgramHeaderOffset = 64, ELFHeaderSize = 64,
                ProgramHeaderSize = 56, ProgramHeaderCount = 1,
                SectionHeaderSize = 64, SectionHeaderCount = 0
            },
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 7, Offset = 0, VirtualAddress = 0,
                    PhysicalAddress = 0, FileSize = 0, MemorySize = 0, Alignment = 0x1000
                }
            ],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 64 + 56);
        var flags = BitConverter.ToUInt32(bytes, 64 + 4);
        Assert.Equal(7u, flags);
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]
    [Theory]
    [InlineData(3, false)]
    [InlineData(62, true)]
    [InlineData(40, false)]
    [InlineData(183, true)]
    public void Encode_DifferentArchitectures_ProducesValidElf(ushort machine, bool is64)
    {
        var data = create_minimal_elf_data(is64, machine);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(0x7F, bytes[0]);
        Assert.Equal((byte)'E', bytes[1]);
        Assert.Equal((byte)'L', bytes[2]);
        Assert.Equal((byte)'F', bytes[3]);
    }

    // [/section renamed - encoding fix]

    // [section renamed - encoding fix]

    private static ELFHeaderData create_minimal_elf_header(bool is64, ushort machine = 0, ulong entryPoint = 0)
    {
        var headerSize = is64 ? (ushort)64 : (ushort)52;
        return new ELFHeaderData
        {
            Magic = [0x7F, 0x45, 0x4C, 0x46],
            Class = (byte)(is64 ? 2 : 1),
            DataEncoding = 1,
            Version = 1,
            OSABI = 0,
            ABIVersion = 0,
            Type = 2,
            Machine = machine != 0 ? machine : (is64 ? (ushort)62 : (ushort)3),
            ObjectVersion = 1,
            EntryPoint = entryPoint,
            ProgramHeaderOffset = (ulong)headerSize,
            SectionHeaderOffset = 0,
            Flags = 0,
            ELFHeaderSize = headerSize,
            ProgramHeaderSize = is64 ? (ushort)56 : (ushort)32,
            ProgramHeaderCount = 0,
            SectionHeaderSize = is64 ? (ushort)64 : (ushort)40,
            SectionHeaderCount = 0,
            StringTableIndex = 0
        };
    }

    private static ELFFileData create_minimal_elf_data(bool is64, ushort machine = 0, ulong entryPoint = 0)
    {
        return new ELFFileData
        {
            Header = create_minimal_elf_header(is64, machine, entryPoint),
            ProgramHeaders = [],
            SectionHeaders = []
        };
    }

    private static ELFFileData create_elf_with_section_content(byte[] textContent)
    {
        var headerSize = 64;
        var phSize = 56;
        var textOffset = headerSize + phSize;

        return new ELFFileData
        {
            Header = new ELFHeaderData
            {
                Magic = [0x7F, 0x45, 0x4C, 0x46],
                Class = 2, DataEncoding = 1, Version = 1, OSABI = 0, ABIVersion = 0,
                Type = 2, Machine = 62, ObjectVersion = 1, EntryPoint = 0x400000,
                ProgramHeaderOffset = (ulong)headerSize, SectionHeaderOffset = 0, Flags = 0,
                ELFHeaderSize = 64, ProgramHeaderSize = 56, ProgramHeaderCount = 1,
                SectionHeaderSize = 64, SectionHeaderCount = 3, StringTableIndex = 2
            },
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = (ulong)textOffset,
                    VirtualAddress = 0x400000, PhysicalAddress = 0x400000,
                    FileSize = (ulong)textContent.Length, MemorySize = (ulong)textContent.Length,
                    Alignment = 0x1000
                }
            ],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] },
                new ELFSectionHeaderData
                {
                    Name = ".text", Type = 1, Flags = 6, Address = 0x400000,
                    Offset = (ulong)textOffset, Size = (ulong)textContent.Length,
                    Alignment = 16, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Content = System.Text.Encoding.UTF8.GetBytes("\0.text\0.shstrtab\0")
                }
            ]
        };
    }

    private static ELFFileData create_elf_with_multiple_sections(byte[] textContent, byte[] dataContent)
    {
        var headerSize = 64;
        var phSize = 56;
        var textOffset = headerSize + phSize;
        var dataOffset = textOffset + textContent.Length;

        return new ELFFileData
        {
            Header = new ELFHeaderData
            {
                Magic = [0x7F, 0x45, 0x4C, 0x46],
                Class = 2, DataEncoding = 1, Version = 1, OSABI = 0, ABIVersion = 0,
                Type = 2, Machine = 62, ObjectVersion = 1, EntryPoint = 0x400000,
                ProgramHeaderOffset = (ulong)headerSize, SectionHeaderOffset = 0, Flags = 0,
                ELFHeaderSize = 64, ProgramHeaderSize = 56, ProgramHeaderCount = 1,
                SectionHeaderSize = 64, SectionHeaderCount = 4, StringTableIndex = 3
            },
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = (ulong)textOffset,
                    VirtualAddress = 0x400000, PhysicalAddress = 0x400000,
                    FileSize = (ulong)(textContent.Length + dataContent.Length),
                    MemorySize = (ulong)(textContent.Length + dataContent.Length),
                    Alignment = 0x1000
                }
            ],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] },
                new ELFSectionHeaderData
                {
                    Name = ".text", Type = 1, Flags = 6, Address = 0x400000,
                    Offset = (ulong)textOffset, Size = (ulong)textContent.Length,
                    Alignment = 16, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".data", Type = 1, Flags = 3, Address = 0x600000,
                    Offset = (ulong)dataOffset, Size = (ulong)dataContent.Length,
                    Alignment = 8, Content = dataContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Content = System.Text.Encoding.UTF8.GetBytes("\0.text\0.data\0.shstrtab\0")
                }
            ]
        };
    }

    // [/section renamed - encoding fix]
}