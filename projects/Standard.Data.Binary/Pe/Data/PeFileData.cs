namespace Std.Data.Binary.Pe.Data;

/// <summary>
///     PE 文件数据的
/// </summary>
public sealed class PeFileData
{
    /// <summary>
    ///     PE 头数据的
    /// </summary>
    public PeHeaderData header { get; init; } = new();

    /// <summary>
    ///     可选头数据的
    /// </summary>
    public PeOptionalHeaderData optional_header { get; init; } = new();

    /// <summary>
    ///     节区列表的
    /// </summary>
    public IReadOnlyList<PeSectionData> sections { get; init; } = [];

    /// <summary>
    ///     节区内容数据，键为节区在 Sections 列表中的索引，值为原始字节数据的
    /// </summary>
    public Dictionary<int, byte[]> section_contents { get; init; } = [];

    /// <summary>
    ///     是否的DLL的
    /// </summary>
    public bool is_dll => (header.characteristics & PeConstants.characteristics_dll) != 0;

    /// <summary>
    ///     是否为可执行文件的
    /// </summary>
    public bool is_executable => (header.characteristics & PeConstants.characteristics_executable) != 0;

    /// <summary>
    ///     是否的64 位的
    /// </summary>
    public bool is64_bit => optional_header.magic == PeConstants.optional_magic_pe32_plus;

    /// <summary>
    ///     导入表列表的
    /// </summary>
    public IReadOnlyList<PeImportDescriptor> imports { get; set; } = [];

    /// <summary>
    ///     重定位块列表的
    /// </summary>
    public IReadOnlyList<PeBaseRelocationBlock> relocations { get; set; } = [];

    /// <summary>
    ///     获取指定索引的数据目录条目。索引不存在时返回空条目的
    /// </summary>
    public PeDirectoryDataEntry get_data_directory(PeDirectoryDataIndex index)
    {
        var i = (int)index;

        if (i < optional_header.data_directories.Count) return optional_header.data_directories[i];

        return new PeDirectoryDataEntry();
    }

    /// <summary>
    ///     的RVA（相对虚拟地址）转换为文件偏移量的
    /// </summary>
    public int rva_to_offset(uint rva)
    {
        foreach (var section in sections)
        {
            var sectionEnd = section.virtual_address + section.virtual_size;

            if (rva >= section.virtual_address && rva < sectionEnd)
                return (int)(rva - section.virtual_address + section.pointer_to_raw_data);
        }

        return (int)rva;
    }
}