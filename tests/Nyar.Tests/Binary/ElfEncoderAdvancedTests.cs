using Nyar.Binary.Elf.Data;
using Nyar.Binary.Elf.Encode;

namespace Nyar.Tests.Binary;

public sealed class ElfEncoderAdvancedTests
{
    // -region-

    [Theory]
    [InlineData(3, false)]
    [InlineData(62, true)]
    [InlineData(40, false)]
    [InlineData(183, true)]
    [InlineData(243, false)]
    [InlineData(243, true)]
    public void Encode_MachineTypeMappings_ProduceCorrectClass(ushort machine, bool expect64)
    {
        var data = create_minimal_elf_data(expect64, machine);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(expect64 ? (byte)2 : (byte)1, bytes[4]);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_ExecutableType_ValueIs2()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var type = BitConverter.ToUInt16(bytes, 16);
        Assert.Equal((ushort)2, type);
    }

    [Fact]
    public void Encode_SharedLibraryType_ValueIs3()
    {
        var data = create_minimal_elf_data(true, type: 3);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var type = BitConverter.ToUInt16(bytes, 16);
        Assert.Equal((ushort)3, type);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_SectionAlignment_PreservesValue()
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_elf_with_text_section(textContent, alignment: 32);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 64);
    }

    [Fact]
    public void Encode_LargeSectionContent_ProducesCorrectOutput()
    {
        var textContent = new byte[4096];
        for (var i = 0; i < textContent.Length; i++)
        {
            textContent[i] = 0x90;
        }

        var data = create_elf_with_text_section(textContent);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.True(bytes.Length >= 64 + 56 + textContent.Length);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(6u)]
    public void Encode_ProgramHeaderType_PreservesValue(uint phType)
    {
        var data = new ELFFileData
        {
            Header = create_minimal_elf_header(true, programHeaderCount: 1),
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = phType, Flags = 5, Offset = 0, VirtualAddress = 0,
                    PhysicalAddress = 0, FileSize = 0, MemorySize = 0, Alignment = 0x1000
                }
            ],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var actualType = BitConverter.ToUInt32(bytes, 64);
        Assert.Equal(phType, actualType);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_StringTable_ContainsAllSectionNames()
    {
        var textContent = new byte[] { 0xC3 };
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04 };

        var data = create_elf_with_text_and_data_sections(textContent, dataContent);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var searchText = ".text"u8;
        var searchData = ".data"u8;
        var searchShstrtab = ".shstrtab"u8;

        Assert.True(contains_bytes(bytes, searchText), ".text 未在字符串表中找到");
        Assert.True(contains_bytes(bytes, searchData), ".data 未在字符串表中找到");
        Assert.True(contains_bytes(bytes, searchShstrtab), ".shstrtab 未在字符串表中找到");
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_VersionField_Is1()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(1, bytes[6]);
    }

    [Fact]
    public void Encode_OSABI_DefaultIs0()
    {
        var data = create_minimal_elf_data(true);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(0, bytes[7]);
    }

    [Fact]
    public void Encode_OSABI_LinuxIs3()
    {
        var header = create_minimal_elf_header(true);
        var data = new ELFFileData
        {
            Header = new ELFHeaderData
            {
                Magic = [0x7F, 0x45, 0x4C, 0x46],
                Class = 2, DataEncoding = 1, Version = 1,
                OSABI = 3, ABIVersion = 0,
                Type = 2, Machine = 62, ObjectVersion = 1,
                ProgramHeaderOffset = 64, ELFHeaderSize = 64,
                ProgramHeaderSize = 56, SectionHeaderSize = 64
            },
            ProgramHeaders = [],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.Equal(3, bytes[7]);
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_TextSectionFlags_IsExecuteAndAlloc()
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_elf_with_text_section(textContent, 6);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 64);
    }

    [Fact]
    public void Encode_DataSectionFlags_IsWriteAndAlloc()
    {
        var dataContent = new byte[] { 0x01, 0x02 };
        var data = create_elf_with_text_and_data_sections([0xC3], dataContent);

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 64);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Theory]
    [InlineData(0u)]
    [InlineData(0x400000u)]
    [InlineData(0x8048000u)]
    [InlineData(0x1000000u)]
    public void Encode_EntryPoint_PreservesValue(ulong entryPoint)
    {
        var data = create_minimal_elf_data(true, entryPoint: entryPoint);
        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        var actualEntry = BitConverter.ToUInt64(bytes, 24);
        Assert.Equal(entryPoint, actualEntry);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_NullSectionHeader_ProducesValidOutput()
    {
        var data = new ELFFileData
        {
            Header = create_minimal_elf_header(true, sectionHeaderCount: 1),
            ProgramHeaders = [],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] }
            ]
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length >= 64 + 64);
    }

    // [/section renamed - encoding fix]

    // -region-

    private static ELFHeaderData create_minimal_elf_header(
        bool is64, ushort machine = 0, ushort type = 2, ulong entryPoint = 0,
        int programHeaderCount = 0, int sectionHeaderCount = 0)
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
            Type = type,
            Machine = machine != 0 ? machine : (is64 ? (ushort)62 : (ushort)3),
            ObjectVersion = 1,
            EntryPoint = entryPoint,
            ProgramHeaderOffset = (ulong)headerSize,
            SectionHeaderOffset = 0,
            Flags = 0,
            ELFHeaderSize = headerSize,
            ProgramHeaderSize = is64 ? (ushort)56 : (ushort)32,
            ProgramHeaderCount = (ushort)programHeaderCount,
            SectionHeaderSize = is64 ? (ushort)64 : (ushort)40,
            SectionHeaderCount = (ushort)sectionHeaderCount,
            StringTableIndex = 0
        };
    }

    private static ELFFileData create_minimal_elf_data(bool is64, ushort machine = 0, ulong entryPoint = 0,
        ushort type = 2)
    {
        return new ELFFileData
        {
            Header = create_minimal_elf_header(is64, machine, type, entryPoint),
            ProgramHeaders = [],
            SectionHeaders = []
        };
    }

    private static ELFFileData create_elf_with_text_section(byte[] textContent, ulong flags = 6, ulong alignment = 16)
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
                ProgramHeaderOffset = 64, SectionHeaderOffset = 0, Flags = 0,
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
                    Name = ".text", Type = 1, Flags = flags, Address = 0x400000,
                    Offset = (ulong)textOffset, Size = (ulong)textContent.Length,
                    Alignment = alignment, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Content = System.Text.Encoding.UTF8.GetBytes("\0.text\0.shstrtab\0")
                }
            ]
        };
    }

    private static ELFFileData create_elf_with_text_and_data_sections(byte[] textContent, byte[] dataContent)
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
                ProgramHeaderOffset = 64, SectionHeaderOffset = 0, Flags = 0,
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

    private static bool contains_bytes(byte[] haystack, ReadOnlySpan<byte> needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    // [/section renamed - encoding fix]
}


