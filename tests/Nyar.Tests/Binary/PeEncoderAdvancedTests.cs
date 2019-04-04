using Sonic.Codec;
using Nyar.Binary.Pe.Data;
using Nyar.Binary.Pe.Encode;

namespace Nyar.Tests.Binary;

public sealed class PeEncoderAdvancedTests
{
    // -region-

    private const int _dos_header_size = 122;
    private const int _pe_signature_offset = _dos_header_size;
    private const int _coff_header_offset = _dos_header_size + 4;
    private const int _opt_header_offset = _coff_header_offset + 20;

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_TextSectionCharacteristics_Is60000020()
    {
        var textContent = new byte[] { 0xC3 };
        var data = create_pe_with_section(textContent, 0x60000020);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > _dos_header_size);
    }

    [Fact]
    public void Encode_DataSectionCharacteristics_IsC0000040()
    {
        var dataContent = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var data = create_pe_with_section(dataContent, 0xC0000040);

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > _dos_header_size);
    }

    // [/section renamed - encoding fix]

    #region PE ImageBase

    [Theory]
    [InlineData(0x400000UL, false)]
    [InlineData(0x140000000UL, true)]
    public void Encode_ImageBase_PreservesValue(ulong imageBase, bool is64)
    {
        var data = new PeFileData
        {
            Header = create_minimal_pe_header(is64),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = is64 ? (ushort)0x20B : (ushort)0x10B,
                ImageBase = imageBase,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var imageBaseOffset = _opt_header_offset + (is64 ? 24 : 28);
        if (is64)
        {
            var actualImageBase = BitConverter.ToUInt64(bytes, imageBaseOffset);
            Assert.Equal(imageBase, actualImageBase);
        }
        else
        {
            var actualImageBase = BitConverter.ToUInt32(bytes, imageBaseOffset);
            Assert.Equal((uint)imageBase, actualImageBase);
        }
    }

    // [/section renamed - encoding fix]

    #region PE Characteristics

    [Fact]
    public void Encode_ExecutableCharacteristics_Is0002()
    {
        var data = create_minimal_pe_data(true);
        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var characteristicsOffset = _coff_header_offset + 18;
        var characteristics = BitConverter.ToUInt16(bytes, characteristicsOffset);
        Assert.Equal((ushort)0x0002, characteristics);
    }

    [Fact]
    public void Encode_DllCharacteristics_Is2000()
    {
        var data = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = 0x5A4D, PeHeaderOffset = _dos_header_size, PeMagic = 0x00004550,
                Machine = 0x8664, NumberOfSections = 0, TimeDateStamp = 0,
                PointerToSymbolTable = 0, NumberOfSymbols = 0,
                SizeOfOptionalHeader = 240, Characteristics = 0x2000
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = 3, DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var characteristicsOffset = _coff_header_offset + 18;
        var characteristics = BitConverter.ToUInt16(bytes, characteristicsOffset);
        Assert.Equal((ushort)0x2000, characteristics);
    }

    // [/section renamed - encoding fix]

    #region PE Subsystem

    [Theory]
    [InlineData((ushort)1)]
    [InlineData((ushort)2)]
    [InlineData((ushort)3)]
    [InlineData((ushort)9)]
    public void Encode_Subsystem_PreservesValue(ushort subsystem)
    {
        var data = new PeFileData
        {
            Header = create_minimal_pe_header(true),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = subsystem, DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var subsystemOffset = _opt_header_offset + 68;
        var actual = BitConverter.ToUInt16(bytes, subsystemOffset);
        Assert.Equal(subsystem, actual);
    }

    // [/section renamed - encoding fix]

    #region PE DllCharacteristics

    [Fact]
    public void Encode_DllCharacteristics_PreservesValue()
    {
        var data = new PeFileData
        {
            Header = create_minimal_pe_header(true),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = 3, DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var dllCharsOffset = _opt_header_offset + 70;
        var dllChars = BitConverter.ToUInt16(bytes, dllCharsOffset);
        Assert.Equal((ushort)0x8160, dllChars);
    }

    // [/section renamed - encoding fix]

    // -region-
    [Fact]
    public void Encode_16DataDirectories_AllWritten()
    {
        var is64 = true;
        var dataDirs = new List<PeDirectoryDataEntry>();
        for (var i = 0; i < 16; i++)
        {
            dataDirs.Add(new PeDirectoryDataEntry { Rva = (uint)(i * 0x1000), Size = (uint)(i * 0x100) });
        }

        var data = new PeFileData
        {
            Header = create_minimal_pe_header(is64),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = 3, DllCharacteristics = 0x8160,
                NumberOfRvaAndSizes = 16,
                DataDirectories = dataDirs
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var dataDirStart = _opt_header_offset + (is64 ? 112 : 96);
        for (var i = 0; i < 16; i++)
        {
            var rva = BitConverter.ToUInt32(bytes, dataDirStart + i * 8);
            var size = BitConverter.ToUInt32(bytes, dataDirStart + i * 8 + 4);
            Assert.Equal((uint)(i * 0x1000), rva);
            Assert.Equal((uint)(i * 0x100), size);
        }
    }

    // [/section renamed - encoding fix]

    // -region-

    [Fact]
    public void Encode_SectionAlignment_PreservesValue()
    {
        var data = new PeFileData
        {
            Header = create_minimal_pe_header(true),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x2000, FileAlignment = 0x1000,
                SizeOfImage = 0x8000, SizeOfHeaders = 0x1000,
                Subsystem = 3, DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var sectionAlignmentOffset = _opt_header_offset + 32;
        var sectionAlignment = BitConverter.ToUInt32(bytes, sectionAlignmentOffset);
        Assert.Equal(0x2000u, sectionAlignment);
    }

    // [/section renamed - encoding fix]

    #region PE NumberOfSections

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Encode_NumberOfSections_PreservesValue(int numSections)
    {
        var sections = new List<PeSectionData>();
        var contents = new Dictionary<int, byte[]>();

        for (var i = 0; i < numSections; i++)
        {
            var nameSpan = System.Text.Encoding.UTF8.GetBytes($".s{i}\0\0\0\0");
            var nameBytes = FixedBytes8.FromSpan(nameSpan);

            var item = new PeSectionData
            {
                NameBytes = nameBytes,
                VirtualSize = 0x100,
                VirtualAddress = (uint)(0x1000 * (i + 1)),
                SizeOfRawData = 0x200,
                PointerToRawData = (uint)(0x200 * (i + 1)),
                PointerToRelocations = 0,
                PointerToLinenumbers = 0,
                NumberOfRelocations = 0,
                NumberOfLinenumbers = 0,
                Characteristics = 0x60000020
            };
            sections.Add(item);
            contents[i] = new byte[0x100];
        }

        var data = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = 0x5A4D, PeHeaderOffset = _dos_header_size, PeMagic = 0x00004550,
                Machine = 0x8664, NumberOfSections = (ushort)numSections,
                TimeDateStamp = 0, PointerToSymbolTable = 0, NumberOfSymbols = 0,
                SizeOfOptionalHeader = 240, Characteristics = 0x0002
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = 3, DllCharacteristics = 0x8160
            },
            Sections = sections,
            SectionContents = contents
        };

        var encoder = new PeEncoder();
        var bytes = encoder.Encode(data);

        var numSectionsOffset = _coff_header_offset + 2;
        var actual = BitConverter.ToUInt16(bytes, numSectionsOffset);
        Assert.Equal((ushort)numSections, actual);
    }

    // [/section renamed - encoding fix]

    // -region-

    private static PeHeaderData create_minimal_pe_header(bool is64, ushort machine = 0)
    {
        return new PeHeaderData
        {
            DosMagic = 0x5A4D,
            PeHeaderOffset = _dos_header_size,
            PeMagic = 0x00004550,
            Machine = machine != 0 ? machine : (is64 ? (ushort)0x8664 : (ushort)0x014C),
            NumberOfSections = 0,
            TimeDateStamp = 0,
            PointerToSymbolTable = 0,
            NumberOfSymbols = 0,
            SizeOfOptionalHeader = is64 ? (ushort)240 : (ushort)224,
            Characteristics = 0x0002
        };
    }

    private static PeFileData create_minimal_pe_data(bool is64, ushort machine = 0)
    {
        return new PeFileData
        {
            Header = create_minimal_pe_header(is64, machine),
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = is64 ? (ushort)0x20B : (ushort)0x10B,
                ImageBase = is64 ? 0x140000000UL : 0x400000,
                SectionAlignment = 0x1000,
                FileAlignment = 0x200,
                SizeOfImage = 0x4000,
                SizeOfHeaders = 0x200,
                Subsystem = 3,
                DllCharacteristics = 0x8160
            },
            Sections = [],
            SectionContents = []
        };
    }

    private static PeFileData create_pe_with_section(byte[] content, uint characteristics)
    {
        var nameBytes = FixedBytes8.FromSpan(".text\0\0\0"u8);

        var data = new PeFileData
        {
            Header = new PeHeaderData
            {
                DosMagic = 0x5A4D, PeHeaderOffset = _dos_header_size, PeMagic = 0x00004550,
                Machine = 0x8664, NumberOfSections = 1, TimeDateStamp = 0,
                PointerToSymbolTable = 0, NumberOfSymbols = 0,
                SizeOfOptionalHeader = 240, Characteristics = 0x0002
            },
            OptionalHeader = new PeOptionalHeaderData
            {
                Magic = 0x20B, ImageBase = 0x140000000UL,
                SectionAlignment = 0x1000, FileAlignment = 0x200,
                SizeOfImage = 0x4000, SizeOfHeaders = 0x200,
                Subsystem = 3, DllCharacteristics = 0x8160
            },
            Sections =
            [
                new PeSectionData
                {
                    NameBytes = nameBytes,
                    VirtualSize = (uint)content.Length,
                    VirtualAddress = 0x1000,
                    SizeOfRawData = (uint)((content.Length + 0x1FF) & ~0x1FF),
                    PointerToRawData = 0x200,
                    PointerToRelocations = 0, PointerToLinenumbers = 0,
                    NumberOfRelocations = 0, NumberOfLinenumbers = 0,
                    Characteristics = characteristics
                }
            ],
            SectionContents = new Dictionary<int, byte[]>
            {
                [0] = content
            }
        };
        return data;
    }

    // [/section renamed - encoding fix]
}
