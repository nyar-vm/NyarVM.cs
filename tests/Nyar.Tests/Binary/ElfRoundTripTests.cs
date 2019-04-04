namespace Nyar.Tests.Binary;

public sealed class ElfRoundTripTests
{
    #region ���ELF32 ����

    [Fact]
    public void Encode_Decode_MinimalElf32()
    {
        var original = new ELFFileData
        {
            Header = create_elf_header(false),
            ProgramHeaders = [],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        assert_header_equal(original.Header, decoded.Header);
        Assert.Empty(decoded.SectionHeaders);
        Assert.Empty(decoded.ProgramHeaders);
    }

    #endregion

    #region ���ELF64 ����

    [Fact]
    public void Encode_Decode_MinimalElf64()
    {
        var original = new ELFFileData
        {
            Header = create_elf_header(true),
            ProgramHeaders = [],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        assert_header_equal(original.Header, decoded.Header);
        Assert.Empty(decoded.SectionHeaders);
        Assert.Empty(decoded.ProgramHeaders);
    }

    #endregion

    #region ���������ݵ�����

    [Fact]
    public void Encode_Decode_WithSectionContent()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 };

        var original = new ELFFileData
        {
            Header = create_elf_header(true, sectionHeaderCount: 4, stringTableIndex: 3),
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = 120,
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
                    Offset = 128, Size = (ulong)textContent.Length,
                    Alignment = 16, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".data", Type = 1, Flags = 3, Address = 0x600000,
                    Offset = 136, Size = (ulong)dataContent.Length, Alignment = 8,
                    Content = dataContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Size = 23, Offset = 144,
                    Content = "\0.text\0.data\0.shstrtab\0"u8.ToArray()
                }
            ]
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        assert_header_equal(original.Header, decoded.Header);
        Assert.Equal(original.SectionHeaders.Count, decoded.SectionHeaders.Count);

        for (var i = 0; i < original.SectionHeaders.Count; i++)
        {
            var orig = original.SectionHeaders[i];
            var dec = decoded.SectionHeaders[i];

            Assert.Equal(orig.type, dec.type);
            Assert.Equal(orig.Flags, dec.Flags);
            Assert.Equal(orig.Address, dec.Address);
            Assert.Equal(orig.Alignment, dec.Alignment);
            Assert.Equal(orig.Size, dec.Size);
            Assert.Equal(orig.Content, dec.Content);
        }
    }

    #endregion

    #region �����ű�������

    [Fact]
    public void Encode_Decode_WithSymbolTable()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0x00, 0xC3 };

        var original = new ELFFileData
        {
            Header = create_elf_header(true, sectionHeaderCount: 3, stringTableIndex: 2),
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = 64 + 56,
                    VirtualAddress = 0x400000, PhysicalAddress = 0x400000,
                    FileSize = (ulong)textContent.Length,
                    MemorySize = (ulong)textContent.Length,
                    Alignment = 0x1000
                }
            ],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] },
                new ELFSectionHeaderData
                {
                    Name = ".text", Type = 1, Flags = 6, Address = 0x400000,
                    Offset = 64 + 56, Size = (ulong)textContent.Length,
                    Alignment = 16, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Content = "\0.text\0.shstrtab\0"u8.ToArray()
                }
            ],
            SymbolTable = new ELFSymbolTableData
            {
                Symbols =
                [
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindLocal << 4) | ElfConstants.SymbolTypeSection),
                        Other = 0, SectionIndex = 1, Value = 0, Size = 0, Name = ".text"
                    },
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindGlobal << 4) | ElfConstants.SymbolTypeFunc),
                        Other = 0, SectionIndex = 1, Value = 0x400000, Size = (ulong)textContent.Length, Name = "main"
                    },
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindGlobal << 4) | ElfConstants.SymbolTypeObject),
                        Other = 0, SectionIndex = ElfConstants.SectionIndexUndefined,
                        Value = 0, Size = 0, Name = "external_func"
                    }
                ]
            }
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        assert_header_equal(original.Header, decoded.Header);
        Assert.NotNull(decoded.SymbolTable);
        Assert.Equal(original.SymbolTable.Symbols.Count, decoded.SymbolTable.Symbols.Count);

        for (var i = 0; i < original.SymbolTable.Symbols.Count; i++)
        {
            var orig = original.SymbolTable.Symbols[i];
            var dec = decoded.SymbolTable.Symbols[i];

            Assert.Equal(orig.Name, dec.Name);
            Assert.Equal(orig.Value, dec.Value);
            Assert.Equal(orig.Size, dec.Size);
            Assert.Equal(orig.Bind, dec.Bind);
            Assert.Equal(orig.type, dec.type);
            Assert.Equal(orig.SectionIndex, dec.SectionIndex);
            Assert.Equal(orig.Other, dec.Other);
        }
    }

    [Fact]
    public void Encode_Decode_WithSymbolTable_32Bit()
    {
        var textContent = new byte[] { 0xB8, 0x01, 0x00, 0x00, 0xC3 };

        var original = new ELFFileData
        {
            Header = create_elf_header(false, sectionHeaderCount: 3, stringTableIndex: 2),
            ProgramHeaders =
            [
                new ELFProgramHeaderData
                {
                    Type = 1, Flags = 5, Offset = 52 + 32,
                    VirtualAddress = 0x8048000, PhysicalAddress = 0x8048000,
                    FileSize = (ulong)textContent.Length,
                    MemorySize = (ulong)textContent.Length,
                    Alignment = 0x1000
                }
            ],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] },
                new ELFSectionHeaderData
                {
                    Name = ".text", Type = 1, Flags = 6, Address = 0x8048000,
                    Offset = 52 + 32, Size = (ulong)textContent.Length,
                    Alignment = 16, Content = textContent
                },
                new ELFSectionHeaderData
                {
                    Name = ".shstrtab", Type = 3, Alignment = 1,
                    Content = "\0.text\0.shstrtab\0"u8.ToArray()
                }
            ],
            SymbolTable = new ELFSymbolTableData
            {
                Symbols =
                [
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindLocal << 4) | ElfConstants.SymbolTypeSection),
                        Other = 0, SectionIndex = 1, Value = 0, Size = 0, Name = ".text"
                    },
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindGlobal << 4) | ElfConstants.SymbolTypeFunc),
                        Other = 0, SectionIndex = 1, Value = 0x8048000, Size = (ulong)textContent.Length,
                        Name = "_start"
                    }
                ]
            }
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        assert_header_equal(original.Header, decoded.Header);
        Assert.NotNull(decoded.SymbolTable);
        Assert.Equal(2, decoded.SymbolTable.Symbols.Count);

        Assert.Equal("_start", decoded.SymbolTable.Symbols[1].Name);
        Assert.Equal((ulong)0x8048000, decoded.SymbolTable.Symbols[1].Value);
    }

    #endregion

    #region ��ܹ�����

    [Theory]
    [InlineData(3, false)]
    [InlineData(62, true)]
    [InlineData(40, false)]
    [InlineData(183, true)]
    [InlineData(243, false)]
    [InlineData(243, true)]
    public void Encode_Decode_MultiArchitecture(ushort machine, bool is64)
    {
        var original = new ELFFileData
        {
            Header = create_elf_header(is64, machine),
            ProgramHeaders = [],
            SectionHeaders = []
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Header.Machine, decoded.Header.Machine);
        Assert.Equal(original.Header.Class, decoded.Header.Class);
        Assert.Equal(original.Header.Is64Bit, decoded.Header.Is64Bit);
        assert_header_equal(original.Header, decoded.Header);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(62, true)]
    [InlineData(40, false)]
    [InlineData(183, true)]
    [InlineData(243, true)]
    public void Encode_Decode_MultiArchitecture_WithSymbolTable(ushort machine, bool is64)
    {
        var original = new ELFFileData
        {
            Header = create_elf_header(is64, machine: machine, sectionHeaderCount: 1, stringTableIndex: 0),
            ProgramHeaders = [],
            SectionHeaders =
            [
                new ELFSectionHeaderData { Name = "", Type = 0, Content = [] }
            ],
            SymbolTable = new ELFSymbolTableData
            {
                Symbols =
                [
                    new ELFSymbolData
                    {
                        NameIndex = 0,
                        Info = (byte)((ElfConstants.SymbolBindGlobal << 4) | ElfConstants.SymbolTypeFunc),
                        Other = 0, SectionIndex = ElfConstants.SectionIndexUndefined,
                        Value = 0x1000, Size = 16, Name = "entry"
                    }
                ]
            }
        };

        var encoder = new ElfEncoder();
        var bytes = encoder.Encode(original);

        var decoder = new ELFDecoder();
        var decoded = decoder.Decode(bytes);

        Assert.Equal(original.Header.Machine, decoded.Header.Machine);
        Assert.NotNull(decoded.SymbolTable);
        Assert.Single(decoded.SymbolTable.Symbols);
        Assert.Equal("entry", decoded.SymbolTable.Symbols[0].Name);
        Assert.Equal((ulong)0x1000, decoded.SymbolTable.Symbols[0].Value);
        Assert.Equal((ulong)16, decoded.SymbolTable.Symbols[0].Size);
    }

    #endregion

    #region ��������

    private static ELFHeaderData create_elf_header(
        bool is64,
        ushort machine = 0,
        ushort type = 2,
        int sectionHeaderCount = 0,
        int stringTableIndex = 0)
    {
        var headerSize = is64 ? (ushort)64 : (ushort)52;
        return new ELFHeaderData
        {
            Magic = [0x7F, 0x45, 0x4C, 0x46],
            Class = (byte)(is64 ? ElfConstants.Class64 : ElfConstants.Class32),
            DataEncoding = ElfConstants.DataEncodingLittleEndian,
            Version = 1,
            OSABI = 0,
            ABIVersion = 0,
            Type = type,
            Machine = machine != 0 ? machine : (is64 ? (ushort)62 : (ushort)3),
            ObjectVersion = 1,
            EntryPoint = 0,
            ProgramHeaderOffset = (ulong)headerSize,
            Flags = 0,
            ELFHeaderSize = headerSize,
            ProgramHeaderSize = is64 ? (ushort)56 : (ushort)32,
            ProgramHeaderCount = 0,
            SectionHeaderSize = is64 ? (ushort)64 : (ushort)40,
            SectionHeaderCount = (ushort)sectionHeaderCount,
            StringTableIndex = (ushort)stringTableIndex
        };
    }

    private static void assert_header_equal(ELFHeaderData expected, ELFHeaderData actual)
    {
        Assert.Equal(expected.Magic, actual.Magic);
        Assert.Equal(expected.Class, actual.Class);
        Assert.Equal(expected.DataEncoding, actual.DataEncoding);
        Assert.Equal(expected.Version, actual.Version);
        Assert.Equal(expected.OSABI, actual.OSABI);
        Assert.Equal(expected.ABIVersion, actual.ABIVersion);
        Assert.Equal(expected.type, actual.type);
        Assert.Equal(expected.Machine, actual.Machine);
        Assert.Equal(expected.ObjectVersion, actual.ObjectVersion);
        Assert.Equal(expected.EntryPoint, actual.EntryPoint);
        Assert.Equal(expected.Flags, actual.Flags);
        Assert.Equal(expected.ELFHeaderSize, actual.ELFHeaderSize);
        Assert.Equal(expected.ProgramHeaderSize, actual.ProgramHeaderSize);
        Assert.Equal(expected.ProgramHeaderCount, actual.ProgramHeaderCount);
        Assert.Equal(expected.SectionHeaderSize, actual.SectionHeaderSize);
    }

    #endregion
}
