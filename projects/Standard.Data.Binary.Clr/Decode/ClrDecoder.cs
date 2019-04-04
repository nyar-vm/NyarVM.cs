using System.Text;
using Std.Data.Binary.Clr.Data;
using Std.Data.Binary.Frame;
using Std.Data.Binary.Pe.Data;
using Std.Data.Binary.Pe.Decode;

namespace Std.Data.Binary.Clr.Decode;

/// <summary>
///     CLR 模块解码器，解析 .NET 程序集（PE + CLR 元数的+ MSIL）的
/// </summary>
public sealed class ClrDecoder
{
    private readonly PeDecoder _pe_decoder = new();

    /// <summary>
    ///     从字节数组解的CLR 模块的
    /// </summary>
    public ClrModuleData decode(ReadOnlySpan<byte> data)
    {
        var peFile = _pe_decoder.decode(data);

        var clrDirectory = decode_clr_directory(data, peFile);
        var metadata = decode_metadata(data, peFile, clrDirectory);

        var moduleRow = resolve_module_row(metadata);
        var typeDefRows = resolve_type_def_rows(metadata);
        var methodDefRows = resolve_method_def_rows(metadata);
        var fieldDefRows = resolve_field_def_rows(metadata);
        var propertyDefRows = resolve_property_def_rows(metadata);
        var eventDefRows = resolve_event_def_rows(metadata);

        var methods = decode_method_bodies(data, peFile, methodDefRows, metadata);

        return new ClrModuleData
        {
            pe_file = peFile,
            clr_directory = clrDirectory,
            metadata = metadata,
            methods = methods,
            types = build_type_defs(typeDefRows, methodDefRows, fieldDefRows, propertyDefRows, eventDefRows, methods,
                metadata),
            fields = fieldDefRows,
            properties = propertyDefRows,
            events = eventDefRows,
            module_name = moduleRow?.name ?? string.Empty,
            version = metadata.header.version_string
        };
    }

    #region CLR 目录解码

    /// <summary>
    ///     解码 CLR 目录表（PE 可选头数据目录索引 14）的
    /// </summary>
    private ClrDirectoryData decode_clr_directory(ReadOnlySpan<byte> data, PeFileData peFile)
    {
        var clrDir = peFile.get_data_directory(PeDirectoryDataIndex.clr_runtime_header);

        if (clrDir.is_empty) throw new InvalidDataException("PE 文件不包含 CLR 目录（不是 .NET 程序集）");

        var offset = peFile.rva_to_offset(clrDir.rva);

        if (offset < 0 || offset + ClrConstants.clr_directory_size > data.Length)
            throw new InvalidDataException("CLR 目录数据超出文件范围");

        var buffer = new ByteBuffer(data[offset..]);

        return new ClrDirectoryData
        {
            cb = buffer.read_u32_le(),
            major_runtime_version = buffer.read_u16_le(),
            minor_runtime_version = buffer.read_u16_le(),
            metadata_rva = buffer.read_u32_le(),
            metadata_size = buffer.read_u32_le(),
            flags = buffer.read_u32_le(),
            entry_point = buffer.read_u32_le(),
            resources_rva = buffer.read_u32_le(),
            resources_size = buffer.read_u32_le(),
            strong_name_signature_rva = buffer.read_u32_le(),
            strong_name_signature_size = buffer.read_u32_le(),
            code_manager_table_rva = buffer.read_u32_le(),
            code_manager_table_size = buffer.read_u32_le(),
            v_table_fixups_rva = buffer.read_u32_le(),
            v_table_fixups_size = buffer.read_u32_le(),
            export_address_table_jumps_rva = buffer.read_u32_le(),
            export_address_table_jumps_size = buffer.read_u32_le(),
            managed_native_header_rva = buffer.read_u32_le(),
            managed_native_header_size = buffer.read_u32_le()
        };
    }

    #endregion

    #region 类型构建

    /// <summary>
    ///     的TypeDef 行构建高级类型视图的
    /// </summary>
    private List<ClrTypeDef> build_type_defs(
        List<ClrTypeDefRow> typeDefRows,
        List<ClrMethodDefRow> methodDefRows,
        List<ClrFieldDefRow> fieldDefRows,
        List<ClrPropertyDefRow> propertyDefRows,
        List<ClrEventDefRow> eventDefRows,
        List<ClrMethodDef> methods,
        ClrMetadata metadata)
    {
        var types = new List<ClrTypeDef>(typeDefRows.Count);

        for (var i = 0; i < typeDefRows.Count; i++)
        {
            var row = typeDefRows[i];
            var nextFieldStart =
                i + 1 < typeDefRows.Count ? typeDefRows[i + 1].field_list_start : fieldDefRows.Count + 1;
            var nextMethodStart =
                i + 1 < typeDefRows.Count ? typeDefRows[i + 1].method_list_start : methodDefRows.Count + 1;

            var typeFields = fieldDefRows.Skip(row.field_list_start - 1).Take(nextFieldStart - row.field_list_start)
                .Select(f => new ClrFieldDef { name = f.name, flags = f.flags, signature_index = f.signature_index })
                .ToList();
            var typeMethods = methods.Skip(row.method_list_start - 1).Take(nextMethodStart - row.method_list_start)
                .ToList();

            types.Add(new ClrTypeDef
            {
                name = row.name,
                @namespace = row.@namespace,
                flags = row.flags,
                extends_index = row.extends_index,
                fields = typeFields,
                methods = typeMethods
            });
        }

        return types;
    }

    #endregion

    #region 元数据解的

    /// <summary>
    ///     解码 CLR 元数据（元数据头 + 流头 + 流数据）的
    /// </summary>
    private ClrMetadata decode_metadata(ReadOnlySpan<byte> data, PeFileData peFile, ClrDirectoryData clrDirectory)
    {
        var metadataOffset = peFile.rva_to_offset(clrDirectory.metadata_rva);

        if (metadataOffset < 0 || metadataOffset + ClrConstants.metadata_header_min_size > data.Length)
            throw new InvalidDataException("元数据 RVA 无效或超出文件范围");

        var buffer = new ByteBuffer(data[metadataOffset..]);

        var header = read_metadata_header(ref buffer);
        var streamHeaders = read_stream_headers(ref buffer, header.streams);

        ClrTableStream? tableStream = null;
        var stringHeap = new ClrStringHeap();
        var blobHeap = new ClrBlobHeap();
        var guidHeap = new ClrGuidHeap();
        var userStringHeap = new ClrUserStringHeap();

        foreach (var sh in streamHeaders)
        {
            var streamOffset = (int)sh.offset;
            var streamEnd = streamOffset + (int)sh.size;

            if (streamOffset < 0 || streamEnd > data.Length - metadataOffset) continue;

            var streamData = data.Slice(metadataOffset + streamOffset, (int)sh.size);

            switch (sh.name)
            {
                case ClrConstants.table_stream_name:
                case ClrConstants.unoptimized_table_stream_name:
                    tableStream = decode_table_stream(streamData);
                    break;
                case ClrConstants.strings_stream_name:
                    stringHeap = new ClrStringHeap { data = [.. streamData] };
                    break;
                case ClrConstants.blob_stream_name:
                    blobHeap = new ClrBlobHeap { data = [.. streamData] };
                    break;
                case ClrConstants.guid_stream_name:
                    guidHeap = new ClrGuidHeap { data = [.. streamData] };
                    break;
                case ClrConstants.user_string_stream_name:
                    userStringHeap = new ClrUserStringHeap { data = [.. streamData] };
                    break;
            }
        }

        return new ClrMetadata
        {
            header = header,
            stream_headers = streamHeaders,
            table_stream = tableStream,
            string_heap = stringHeap,
            blob_heap = blobHeap,
            guid_heap = guidHeap,
            user_string_heap = userStringHeap
        };
    }

    /// <summary>
    ///     读取元数据头的
    /// </summary>
    private ClrMetadataHeader read_metadata_header(ref ByteBuffer buffer)
    {
        var signature = buffer.read_u32_le();

        if (signature != ClrConstants.metadata_signature)
            throw new InvalidDataException($"元数据签名不匹配：期的0x{ClrConstants.metadata_signature:X8}，实的0x{signature:X8}");

        var majorVersion = buffer.read_u16_le();
        var minorVersion = buffer.read_u16_le();
        var reserved = buffer.read_u32_le();
        var versionStringLength = buffer.read_u32_le();

        var versionBytes = buffer.read_bytes((int)versionStringLength);
        var versionString = Encoding.UTF8.GetString(versionBytes).TrimEnd('\0');

        var flags = buffer.read_u16_le();
        var streams = buffer.read_u16_le();

        return new ClrMetadataHeader
        {
            signature = signature,
            major_version = majorVersion,
            minor_version = minorVersion,
            reserved = reserved,
            version_string_length = versionStringLength,
            version_string = versionString,
            flags = flags,
            streams = streams
        };
    }

    /// <summary>
    ///     读取流头列表的
    /// </summary>
    private List<ClrStreamHeader> read_stream_headers(ref ByteBuffer buffer, int count)
    {
        var headers = new List<ClrStreamHeader>(count);

        for (var i = 0; i < count; i++)
        {
            var offset = buffer.read_u32_le();
            var size = buffer.read_u32_le();
            var name = read_aligned_stream_name(ref buffer);

            headers.Add(new ClrStreamHeader { offset = offset, size = size, name = name });
        }

        return headers;
    }

    /// <summary>
    ///     读取流名称（null 终止的 字节对齐）的
    /// </summary>
    private static string read_aligned_stream_name(ref ByteBuffer buffer)
    {
        var start = buffer.position;
        var name = buffer.read_null_terminated_string();
        var bytesRead = buffer.position - start;
        var padding = (4 - bytesRead % 4) % 4;
        buffer.advance(padding);

        return name;
    }

    #endregion

    #region 表流解码

    /// <summary>
    ///     解码表流的~ 的#-）的
    /// </summary>
    private ClrTableStream decode_table_stream(ReadOnlySpan<byte> data)
    {
        var buffer = new ByteBuffer(data);

        var reserved = buffer.read_u32_le();
        var majorVersion = buffer.read_u8();
        var minorVersion = buffer.read_u8();
        var heapSizes = buffer.read_u8();
        buffer.read_u8();

        var validTables = buffer.read_u64_le();
        var sortedTables = buffer.read_u64_le();

        var rowCountMask = validTables;
        var rowCounts = new List<uint>();

        for (var i = 0; i < 64; i++)
            if ((rowCountMask & (1UL << i)) != 0)
                rowCounts.Add(buffer.read_u32_le());

        var header = new ClrTableHeader
        {
            reserved = reserved,
            major_version = majorVersion,
            minor_version = minorVersion,
            heap_sizes = heapSizes,
            valid_tables = validTables,
            sorted_tables = sortedTables,
            row_counts = rowCounts
        };

        var tables = decode_tables(ref buffer, header);

        return new ClrTableStream
        {
            header = header,
            tables = tables
        };
    }

    /// <summary>
    ///     解码所有元数据表的
    /// </summary>
    private List<ClrTableData> decode_tables(ref ByteBuffer buffer, ClrTableHeader header)
    {
        var tables = new List<ClrTableData>();
        var rowIndex = 0;

        for (var i = 0; i < 64; i++)
        {
            if ((header.valid_tables & (1UL << i)) == 0) continue;

            var kind = (ClrTableKind)i;
            var rowCount = header.row_counts[rowIndex++];
            var rowSize = get_row_size(kind, header);

            var rawRows = new List<byte[]>((int)rowCount);

            for (var r = 0; r < rowCount; r++)
            {
                if (buffer.remaining < rowSize) break;

                rawRows.Add([.. buffer.read_bytes(rowSize)]);
            }

            tables.Add(new ClrTableData
            {
                kind = kind,
                row_count = rowCount,
                raw_rows = rawRows
            });
        }

        return tables;
    }

    /// <summary>
    ///     计算指定表的单行大小（字节）的
    /// </summary>
    private int get_row_size(ClrTableKind kind, ClrTableHeader header)
    {
        return kind switch
        {
            ClrTableKind.module => 2 + header.string_index_size + header.guid_index_size + header.guid_index_size +
                                   header.guid_index_size,
            ClrTableKind.type_ref => get_coded_index_size(ClrCodedIndex.resolution_scope, header) +
                                     header.string_index_size +
                                     header.string_index_size,
            ClrTableKind.type_def => 4 + header.string_index_size + header.string_index_size +
                                     get_coded_index_size(ClrCodedIndex.type_def_or_ref, header) +
                                     get_table_index_size(ClrTableKind.field, header) +
                                     get_table_index_size(ClrTableKind.method_def, header),
            ClrTableKind.field => 2 + header.string_index_size + header.blob_index_size,
            ClrTableKind.method_def => 4 + 2 + 2 + header.string_index_size + header.blob_index_size +
                                       get_table_index_size(ClrTableKind.param, header),
            ClrTableKind.param => 2 + 2 + header.string_index_size,
            ClrTableKind.interface_impl => get_table_index_size(ClrTableKind.type_def, header) +
                                           get_coded_index_size(ClrCodedIndex.type_def_or_ref, header),
            ClrTableKind.member_ref => get_coded_index_size(ClrCodedIndex.member_ref_parent, header) +
                                       header.string_index_size + header.blob_index_size,
            ClrTableKind.constant => 2 + get_coded_index_size(ClrCodedIndex.has_constant, header) +
                                     header.blob_index_size,
            ClrTableKind.custom_attribute => get_coded_index_size(ClrCodedIndex.has_custom_attribute, header) +
                                             get_coded_index_size(ClrCodedIndex.custom_attribute_type, header) +
                                             header.blob_index_size,
            ClrTableKind.field_marshal => get_coded_index_size(ClrCodedIndex.has_field_marshal, header) +
                                          header.blob_index_size,
            ClrTableKind.decl_security => 2 + get_coded_index_size(ClrCodedIndex.has_decl_security, header) +
                                          header.blob_index_size,
            ClrTableKind.class_layout => 2 + 4 + get_table_index_size(ClrTableKind.type_def, header),
            ClrTableKind.field_layout => 4 + get_table_index_size(ClrTableKind.field, header),
            ClrTableKind.stand_alone_sig => header.blob_index_size,
            ClrTableKind.event_map => get_table_index_size(ClrTableKind.type_def, header) +
                                      get_table_index_size(ClrTableKind.@event, header),
            ClrTableKind.@event => 2 + header.string_index_size +
                                   get_coded_index_size(ClrCodedIndex.type_def_or_ref, header),
            ClrTableKind.property_map => get_table_index_size(ClrTableKind.type_def, header) +
                                         get_table_index_size(ClrTableKind.property, header),
            ClrTableKind.property => 2 + header.string_index_size + header.blob_index_size,
            ClrTableKind.method_semantics => 2 + get_table_index_size(ClrTableKind.method_def, header) +
                                             get_coded_index_size(ClrCodedIndex.has_semantics, header),
            ClrTableKind.method_impl => get_table_index_size(ClrTableKind.type_def, header) +
                                        get_coded_index_size(ClrCodedIndex.method_def_or_ref, header) +
                                        get_coded_index_size(ClrCodedIndex.method_def_or_ref, header),
            ClrTableKind.module_ref => header.string_index_size,
            ClrTableKind.type_spec => header.blob_index_size,
            ClrTableKind.impl_map => 2 + get_coded_index_size(ClrCodedIndex.member_forwarded, header) +
                                     header.string_index_size + get_table_index_size(ClrTableKind.module_ref, header),
            ClrTableKind.field_rva => 4 + get_table_index_size(ClrTableKind.field, header),
            ClrTableKind.assembly => 4 + 2 + 2 + 2 + 2 + 4 + header.blob_index_size + header.string_index_size +
                                     header.string_index_size,
            ClrTableKind.assembly_ref => 2 + 2 + 2 + 2 + 4 + header.blob_index_size + header.string_index_size +
                                         header.string_index_size + header.blob_index_size,
            ClrTableKind.file => 4 + header.string_index_size + header.blob_index_size,
            ClrTableKind.exported_type => 4 + 4 + header.string_index_size + header.string_index_size +
                                          get_coded_index_size(ClrCodedIndex.implementation, header),
            ClrTableKind.manifest_resource => 4 + 4 + get_coded_index_size(ClrCodedIndex.implementation, header) +
                                              header.string_index_size,
            ClrTableKind.nested_class => get_table_index_size(ClrTableKind.type_def, header) +
                                         get_table_index_size(ClrTableKind.type_def, header),
            ClrTableKind.generic_param => 2 + 2 + get_coded_index_size(ClrCodedIndex.type_or_method_def, header) +
                                          header.string_index_size,
            ClrTableKind.method_spec => get_coded_index_size(ClrCodedIndex.method_def_or_ref, header) +
                                        header.blob_index_size,
            ClrTableKind.generic_param_constraint => get_table_index_size(ClrTableKind.generic_param, header) +
                                                     get_coded_index_size(ClrCodedIndex.type_def_or_ref, header),
            _ => 0
        };
    }

    /// <summary>
    ///     获取编码索引大小的 的4 字节）的
    /// </summary>
    private int get_coded_index_size(ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        var maxRows = get_max_rows_for_coded_index(codedIndex, header);
        var tagBits = get_tag_bits_for_coded_index(codedIndex);

        return maxRows << tagBits <= 0xFFFF ? 2 : 4;
    }

    /// <summary>
    ///     获取表索引大小（2 的4 字节）的
    /// </summary>
    private int get_table_index_size(ClrTableKind table, ClrTableHeader header)
    {
        var rowCount = get_row_count(table, header);

        return rowCount <= 0xFFFF ? 2 : 4;
    }

    /// <summary>
    ///     获取指定表的行数的
    /// </summary>
    private uint get_row_count(ClrTableKind table, ClrTableHeader header)
    {
        var rowIndex = 0;

        for (var i = 0; i < 64; i++)
        {
            if ((header.valid_tables & (1UL << i)) == 0) continue;

            if (i == (int)table) return header.row_counts[rowIndex];

            rowIndex++;
        }

        return 0;
    }

    /// <summary>
    ///     获取编码索引涉及的最大行数的
    /// </summary>
    private uint get_max_rows_for_coded_index(ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        var tables = get_tables_for_coded_index(codedIndex);
        var max = 0u;

        foreach (var t in tables)
        {
            var count = get_row_count(t, header);

            if (count > max) max = count;
        }

        return max;
    }

    /// <summary>
    ///     获取编码索引的标签位数的
    /// </summary>
    private static int get_tag_bits_for_coded_index(ClrCodedIndex codedIndex)
    {
        var tables = get_tables_for_coded_index(codedIndex);
        var count = tables.Length;

        if (count <= 1) return 0;

        var bits = 0;

        while (1 << bits < count) bits++;

        return bits;
    }

    /// <summary>
    ///     获取编码索引涉及的表列表的
    /// </summary>
    private static ClrTableKind[] get_tables_for_coded_index(ClrCodedIndex codedIndex)
    {
        return codedIndex switch
        {
            ClrCodedIndex.type_def_or_ref => [ClrTableKind.type_def, ClrTableKind.type_ref, ClrTableKind.type_spec],
            ClrCodedIndex.has_constant => [ClrTableKind.field, ClrTableKind.param, ClrTableKind.property],
            ClrCodedIndex.has_custom_attribute =>
            [
                ClrTableKind.method_def, ClrTableKind.field, ClrTableKind.type_ref, ClrTableKind.type_def,
                ClrTableKind.param, ClrTableKind.interface_impl, ClrTableKind.member_ref, ClrTableKind.module,
                ClrTableKind.decl_security, ClrTableKind.property, ClrTableKind.@event, ClrTableKind.stand_alone_sig,
                ClrTableKind.module_ref, ClrTableKind.type_spec, ClrTableKind.assembly, ClrTableKind.assembly_ref,
                ClrTableKind.file, ClrTableKind.exported_type, ClrTableKind.manifest_resource,
                ClrTableKind.generic_param,
                ClrTableKind.generic_param_constraint, ClrTableKind.method_spec
            ],
            ClrCodedIndex.has_field_marshal => [ClrTableKind.field, ClrTableKind.param],
            ClrCodedIndex.has_decl_security => [ClrTableKind.type_def, ClrTableKind.method_def, ClrTableKind.assembly],
            ClrCodedIndex.member_ref_parent =>
            [
                ClrTableKind.type_def, ClrTableKind.type_ref, ClrTableKind.module_ref, ClrTableKind.method_def,
                ClrTableKind.type_spec
            ],
            ClrCodedIndex.has_semantics => [ClrTableKind.@event, ClrTableKind.property],
            ClrCodedIndex.method_def_or_ref => [ClrTableKind.method_def, ClrTableKind.member_ref],
            ClrCodedIndex.member_forwarded => [ClrTableKind.field, ClrTableKind.method_def],
            ClrCodedIndex.implementation => [ClrTableKind.file, ClrTableKind.assembly_ref, ClrTableKind.exported_type],
            ClrCodedIndex.custom_attribute_type =>
                [ClrTableKind.method_def, ClrTableKind.member_ref, ClrTableKind.method_def],
            ClrCodedIndex.resolution_scope =>
                [ClrTableKind.module, ClrTableKind.module_ref, ClrTableKind.assembly_ref, ClrTableKind.type_ref],
            ClrCodedIndex.type_or_method_def => [ClrTableKind.type_def, ClrTableKind.method_def],
            _ => []
        };
    }

    #endregion

    #region 表行解析

    /// <summary>
    ///     从原始行数据解析 Module 表行的
    /// </summary>
    private ClrModuleRow? resolve_module_row(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.module);

        if (tableData == null || tableData.raw_rows.Count == 0) return null;

        var header = metadata.table_stream!.header;
        var row = tableData.raw_rows[0];
        var buf = new ByteBuffer(row);

        return new ClrModuleRow
        {
            generation = buf.read_u16_le(),
            name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
            mvid = metadata.guid_heap.read_guid(read_index(ref buf, header.guid_index_size)),
            enc_id = metadata.guid_heap.read_guid(read_index(ref buf, header.guid_index_size)),
            enc_base_id = metadata.guid_heap.read_guid(read_index(ref buf, header.guid_index_size))
        };
    }

    /// <summary>
    ///     解析 TypeDef 表行的
    /// </summary>
    private List<ClrTypeDefRow> resolve_type_def_rows(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.type_def);
        var header = metadata.table_stream?.header;
        var result = new List<ClrTypeDefRow>();

        if (tableData == null || header == null) return result;

        foreach (var row in tableData.raw_rows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrTypeDefRow
            {
                flags = (ClrTypeAttributes)buf.read_u32_le(),
                name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                @namespace = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                extends_index = read_coded_index(ref buf, ClrCodedIndex.type_def_or_ref, header),
                field_list_start = (int)read_table_index(ref buf, ClrTableKind.field, header),
                method_list_start = (int)read_table_index(ref buf, ClrTableKind.method_def, header)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 MethodDef 表行的
    /// </summary>
    private List<ClrMethodDefRow> resolve_method_def_rows(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.method_def);
        var header = metadata.table_stream?.header;
        var result = new List<ClrMethodDefRow>();

        if (tableData == null || header == null) return result;

        foreach (var row in tableData.raw_rows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrMethodDefRow
            {
                rva = buf.read_u32_le(),
                impl_flags = buf.read_u16_le(),
                flags = (ClrMethodAttributes)buf.read_u16_le(),
                name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                signature_index = read_index(ref buf, header.blob_index_size),
                param_list_start = (int)read_table_index(ref buf, ClrTableKind.param, header)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Field 表行的
    /// </summary>
    private List<ClrFieldDefRow> resolve_field_def_rows(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.field);
        var header = metadata.table_stream?.header;
        var result = new List<ClrFieldDefRow>();

        if (tableData == null || header == null) return result;

        foreach (var row in tableData.raw_rows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrFieldDefRow
            {
                flags = (ClrFieldAttributes)buf.read_u16_le(),
                name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                signature_index = read_index(ref buf, header.blob_index_size)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Property 表行的
    /// </summary>
    private List<ClrPropertyDefRow> resolve_property_def_rows(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.property);
        var header = metadata.table_stream?.header;
        var result = new List<ClrPropertyDefRow>();

        if (tableData == null || header == null) return result;

        foreach (var row in tableData.raw_rows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrPropertyDefRow
            {
                flags = buf.read_u16_le(),
                name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                signature_index = read_index(ref buf, header.blob_index_size)
            });
        }

        return result;
    }

    /// <summary>
    ///     解析 Event 表行的
    /// </summary>
    private List<ClrEventDefRow> resolve_event_def_rows(ClrMetadata metadata)
    {
        var tableData = find_table(metadata, ClrTableKind.@event);
        var header = metadata.table_stream?.header;
        var result = new List<ClrEventDefRow>();

        if (tableData == null || header == null) return result;

        foreach (var row in tableData.raw_rows)
        {
            var buf = new ByteBuffer(row);

            result.Add(new ClrEventDefRow
            {
                event_flags = buf.read_u16_le(),
                name = metadata.string_heap.read_string(read_index(ref buf, header.string_index_size)),
                event_type_index = read_coded_index(ref buf, ClrCodedIndex.type_def_or_ref, header)
            });
        }

        return result;
    }

    #endregion

    #region MSIL 方法体解的

    /// <summary>
    ///     解码方法体（MSIL 指令 + 异常处理表）的
    /// </summary>
    private List<ClrMethodDef> decode_method_bodies(ReadOnlySpan<byte> data, PeFileData peFile,
        List<ClrMethodDefRow> methodRows, ClrMetadata metadata)
    {
        var methods = new List<ClrMethodDef>(methodRows.Count);

        foreach (var row in methodRows)
        {
            if (row.rva == 0)
            {
                methods.Add(new ClrMethodDef
                {
                    name = row.name,
                    flags = row.flags,
                    rva = 0,
                    code_size = 0,
                    max_stack = 0,
                    local_var_sig_tok = 0
                });

                continue;
            }

            var offset = peFile.rva_to_offset(row.rva);

            if (offset < 0 || offset >= data.Length)
            {
                methods.Add(new ClrMethodDef
                {
                    name = row.name,
                    flags = row.flags,
                    rva = row.rva
                });

                continue;
            }

            var methodBody = decode_method_body(data[offset..]);
            var instructions = decode_instructions(methodBody.code);

            methods.Add(new ClrMethodDef
            {
                name = row.name,
                flags = row.flags,
                rva = row.rva,
                code_size = methodBody.code_size,
                max_stack = methodBody.max_stack,
                local_var_sig_tok = methodBody.local_var_sig_tok,
                instructions = instructions,
                exception_handlers = methodBody.exception_handlers
            });
        }

        return methods;
    }

    /// <summary>
    ///     解码方法头和代码体的
    /// </summary>
    private MethodBodyData decode_method_body(ReadOnlySpan<byte> data)
    {
        if (data.Length < 1) return new MethodBodyData();

        var firstByte = data[0];
        var format = (byte)(firstByte & ClrConstants.method_header_format_mask);

        if (format == ClrConstants.method_header_tiny_flag)
        {
            var codeSize = (byte)(firstByte >> 2);
            var code = codeSize > 0 && codeSize < data.Length ? data[1..(1 + codeSize)] : [];

            return new MethodBodyData
            {
                code_size = codeSize,
                max_stack = 8,
                local_var_sig_tok = 0,
                code = [.. code],
                exception_handlers = []
            };
        }

        if (format == ClrConstants.method_header_fat_flag)
        {
            var buffer = new ByteBuffer(data);
            var headerWord = buffer.read_u16_le();
            var maxStack = buffer.read_u16_le();
            var codeSize = buffer.read_u32_le();
            var localVarSigTok = buffer.read_u32_le();

            var hasMoreSects = (headerWord & ClrConstants.method_header_more_sects) != 0;
            var code = codeSize > 0 && 12 + codeSize <= data.Length ? data[12..(12 + (int)codeSize)] : [];

            var exceptionHandlers = new List<ClrExceptionHandler>();

            if (hasMoreSects)
            {
                var sectOffset = 12 + (int)codeSize;
                align_to4(ref sectOffset);

                while (sectOffset + 4 <= data.Length)
                {
                    var sectData = data[sectOffset..];
                    var sectKind = sectData[0];
                    var isFat = (sectKind & ClrConstants.exception_handler_fat_flag) != 0;
                    var moreSects = (sectKind & 0x80) != 0;

                    if ((sectKind & ClrConstants.exception_handler_table_flag) == 0) break;

                    if (isFat)
                        read_fat_exception_handlers(sectData, exceptionHandlers);
                    else
                        read_small_exception_handlers(sectData, exceptionHandlers);

                    if (!moreSects) break;

                    if (isFat)
                    {
                        var dataSize = (sectData[1] << 16) | (sectData[2] << 8) | sectData[3];
                        sectOffset += dataSize;
                    }
                    else
                    {
                        var dataSize = sectData[1];
                        sectOffset += dataSize;
                    }

                    align_to4(ref sectOffset);
                }
            }

            return new MethodBodyData
            {
                code_size = codeSize,
                max_stack = maxStack,
                local_var_sig_tok = localVarSigTok,
                code = [.. code],
                exception_handlers = exceptionHandlers
            };
        }

        return new MethodBodyData();
    }

    /// <summary>
    ///     读取 Small 异常处理表的
    /// </summary>
    private static void read_small_exception_handlers(ReadOnlySpan<byte> sectData, List<ClrExceptionHandler> handlers)
    {
        var dataSize = sectData[1];
        var clauseSize = 12;
        var clauseCount = (dataSize - 4) / clauseSize;

        var buffer = new ByteBuffer(sectData[4..]);

        for (var i = 0; i < clauseCount; i++)
        {
            var kind = (ClrExceptionHandlerKind)buffer.read_u32_le();
            var tryOffset = buffer.read_u16_le();
            var tryLength = buffer.read_u8();
            var handlerOffset = buffer.read_u16_le();
            var handlerLength = buffer.read_u8();
            var classTokenOrFilter = buffer.read_u32_le();

            handlers.Add(new ClrExceptionHandler
            {
                handler_kind = kind,
                try_start = tryOffset,
                try_length = tryLength,
                handler_start = handlerOffset,
                handler_length = handlerLength,
                class_token_or_filter_offset = classTokenOrFilter
            });
        }
    }

    /// <summary>
    ///     读取 Fat 异常处理表的
    /// </summary>
    private static void read_fat_exception_handlers(ReadOnlySpan<byte> sectData, List<ClrExceptionHandler> handlers)
    {
        var dataSize = (sectData[1] << 16) | (sectData[2] << 8) | sectData[3];
        var clauseSize = 24;
        var clauseCount = (dataSize - 4) / clauseSize;

        var buffer = new ByteBuffer(sectData[4..]);

        for (var i = 0; i < clauseCount; i++)
        {
            var kind = (ClrExceptionHandlerKind)buffer.read_u32_le();
            var tryOffset = buffer.read_u32_le();
            var tryLength = buffer.read_u32_le();
            var handlerOffset = buffer.read_u32_le();
            var handlerLength = buffer.read_u32_le();
            var classTokenOrFilter = buffer.read_u32_le();

            handlers.Add(new ClrExceptionHandler
            {
                handler_kind = kind,
                try_start = tryOffset,
                try_length = tryLength,
                handler_start = handlerOffset,
                handler_length = handlerLength,
                class_token_or_filter_offset = classTokenOrFilter
            });
        }
    }

    /// <summary>
    ///     解码 MSIL 指令的
    /// </summary>
    private List<ClrInstruction> decode_instructions(byte[] code)
    {
        var instructions = new List<ClrInstruction>();
        var buffer = new ByteBuffer(code);
        var offset = 0u;

        while (!buffer.is_end)
        {
            var instrOffset = offset;
            var firstByte = buffer.read_u8();
            ClrOpcode opcode;

            if (firstByte == ClrConstants.two_byte_opcode_prefix)
            {
                var secondByte = buffer.read_u8();
                opcode = (ClrOpcode)(ClrConstants.two_byte_opcode_base | secondByte);
                offset += 2;
            }
            else
            {
                opcode = (ClrOpcode)firstByte;
                offset += 1;
            }

            var operand = decode_operand(ref buffer, opcode, ref offset);

            instructions.Add(new ClrInstruction
            {
                offset = instrOffset,
                opcode = opcode,
                operand = operand
            });
        }

        return instructions;
    }

    /// <summary>
    ///     解码操作数的
    /// </summary>
    private ClrOperand? decode_operand(ref ByteBuffer buffer, ClrOpcode opcode, ref uint offset)
    {
        switch (opcode)
        {
            case ClrOpcode.nop:
            case ClrOpcode.@break:
            case ClrOpcode.ldarg_0:
            case ClrOpcode.ldarg_1:
            case ClrOpcode.ldarg_2:
            case ClrOpcode.ldarg_3:
            case ClrOpcode.ldloc_0:
            case ClrOpcode.ldloc_1:
            case ClrOpcode.ldloc_2:
            case ClrOpcode.ldloc_3:
            case ClrOpcode.stloc_0:
            case ClrOpcode.stloc_1:
            case ClrOpcode.stloc_2:
            case ClrOpcode.stloc_3:
            case ClrOpcode.ldnull:
            case ClrOpcode.ldc_i4_0:
            case ClrOpcode.ldc_i4_1:
            case ClrOpcode.ldc_i4_2:
            case ClrOpcode.ldc_i4_3:
            case ClrOpcode.ldc_i4_4:
            case ClrOpcode.ldc_i4_5:
            case ClrOpcode.ldc_i4_6:
            case ClrOpcode.ldc_i4_7:
            case ClrOpcode.ldc_i4_8:
            case ClrOpcode.ldc_i4_m1:
            case ClrOpcode.dup:
            case ClrOpcode.pop:
            case ClrOpcode.ret:
            case ClrOpcode.add:
            case ClrOpcode.sub:
            case ClrOpcode.mul:
            case ClrOpcode.div:
            case ClrOpcode.div_un:
            case ClrOpcode.rem:
            case ClrOpcode.rem_un:
            case ClrOpcode.and:
            case ClrOpcode.or:
            case ClrOpcode.xor:
            case ClrOpcode.shl:
            case ClrOpcode.shr:
            case ClrOpcode.shr_un:
            case ClrOpcode.neg:
            case ClrOpcode.not:
            case ClrOpcode.conv_i1:
            case ClrOpcode.conv_i2:
            case ClrOpcode.conv_i4:
            case ClrOpcode.conv_i8:
            case ClrOpcode.conv_r4:
            case ClrOpcode.conv_r8:
            case ClrOpcode.conv_u4:
            case ClrOpcode.conv_u8:
            case ClrOpcode.conv_r_un:
            case ClrOpcode.conv_u:
            case ClrOpcode.conv_ovf_i1:
            case ClrOpcode.conv_ovf_u1:
            case ClrOpcode.conv_ovf_i2:
            case ClrOpcode.conv_ovf_u2:
            case ClrOpcode.conv_ovf_i4:
            case ClrOpcode.conv_ovf_u4:
            case ClrOpcode.conv_ovf_i8:
            case ClrOpcode.conv_ovf_u8:
            case ClrOpcode.conv_ovf_i:
            case ClrOpcode.conv_ovf_u:
            case ClrOpcode.conv_ovf_i1_un:
            case ClrOpcode.conv_ovf_i2_un:
            case ClrOpcode.conv_ovf_i4_un:
            case ClrOpcode.conv_ovf_i8_un:
            case ClrOpcode.conv_ovf_u1_un:
            case ClrOpcode.conv_ovf_u2_un:
            case ClrOpcode.conv_ovf_u4_un:
            case ClrOpcode.conv_ovf_u8_un:
            case ClrOpcode.conv_ovf_i_un:
            case ClrOpcode.conv_ovf_u_un:
            case ClrOpcode.ldind_i1:
            case ClrOpcode.ldind_u1:
            case ClrOpcode.ldind_i2:
            case ClrOpcode.ldind_u2:
            case ClrOpcode.ldind_i4:
            case ClrOpcode.ldind_u4:
            case ClrOpcode.ldind_i8:
            case ClrOpcode.ldind_i:
            case ClrOpcode.ldind_r4:
            case ClrOpcode.ldind_r8:
            case ClrOpcode.ldind_ref:
            case ClrOpcode.stind_ref:
            case ClrOpcode.stind_i1:
            case ClrOpcode.stind_i2:
            case ClrOpcode.stind_i4:
            case ClrOpcode.stind_i8:
            case ClrOpcode.stind_r4:
            case ClrOpcode.stind_r8:
            case ClrOpcode.stind_i:
            case ClrOpcode.@throw:
            case ClrOpcode.endfilter:
            case ClrOpcode.rethrow:
            case ClrOpcode.cpblk:
            case ClrOpcode.initblk:
            case ClrOpcode.ldlen:
            case ClrOpcode.arglist:
            case ClrOpcode.ceq:
            case ClrOpcode.cgt:
            case ClrOpcode.cgt_un:
            case ClrOpcode.clt:
            case ClrOpcode.clt_un:
            case ClrOpcode.localloc:
            case ClrOpcode.@readonly:
            case ClrOpcode.@volatile:
            case ClrOpcode.tail:
                return null;

            case ClrOpcode.ldarg_s:
            case ClrOpcode.ldarga_s:
            case ClrOpcode.starg_s:
            case ClrOpcode.ldloc_s:
            case ClrOpcode.ldloca_s:
            case ClrOpcode.stloc_s:
            {
                var val = buffer.read_u8();
                offset += 1;
                return opcode is ClrOpcode.ldarg_s or ClrOpcode.ldarga_s or ClrOpcode.starg_s
                    ? new ClrArgumentIndexOperand { index = val }
                    : new ClrLocalIndexOperand { index = val };
            }

            case ClrOpcode.ldarg:
            case ClrOpcode.ldarga:
            case ClrOpcode.starg:
            {
                var val = buffer.read_u16_le();
                offset += 2;
                return opcode == ClrOpcode.starg
                    ? new ClrArgumentIndexOperand { index = val }
                    : new ClrArgumentIndexOperand { index = val };
            }

            case ClrOpcode.ldloc:
            case ClrOpcode.ldloca:
            case ClrOpcode.stloc:
            {
                var val = buffer.read_u16_le();
                offset += 2;
                return new ClrLocalIndexOperand { index = val };
            }

            case ClrOpcode.ldc_i4_s:
            {
                var val = buffer.read_i8();
                offset += 1;
                return new ClrInt8Operand { value = val };
            }

            case ClrOpcode.ldc_i4:
            {
                var val = buffer.read_i32_le();
                offset += 4;
                return new ClrInt32Operand { value = val };
            }

            case ClrOpcode.ldc_i8:
            {
                var val = buffer.read_i64_le();
                offset += 8;
                return new ClrInt64Operand { value = val };
            }

            case ClrOpcode.ldc_r4:
            {
                var val = buffer.read_f32_le();
                offset += 4;
                return new ClrFloat32Operand { value = val };
            }

            case ClrOpcode.ldc_r8:
            {
                var val = buffer.read_f64_le();
                offset += 8;
                return new ClrFloat64Operand { value = val };
            }

            case ClrOpcode.br_s:
            case ClrOpcode.brfalse_s:
            case ClrOpcode.brtrue_s:
            case ClrOpcode.beq_s:
            case ClrOpcode.bne_un_s:
            case ClrOpcode.blt_s:
            case ClrOpcode.ble_s:
            case ClrOpcode.bgt_s:
            case ClrOpcode.bge_s:
            case ClrOpcode.blt_un_s:
            case ClrOpcode.ble_un_s:
            case ClrOpcode.bgt_un_s:
            case ClrOpcode.bge_un_s:
            case ClrOpcode.leave_s:
            {
                var delta = buffer.read_i8();
                offset += 1;
                return new ClrBranchTarget8Operand { offset = (int)offset + delta };
            }

            case ClrOpcode.br:
            case ClrOpcode.brfalse:
            case ClrOpcode.brtrue:
            case ClrOpcode.beq:
            case ClrOpcode.bne_un:
            case ClrOpcode.blt:
            case ClrOpcode.ble:
            case ClrOpcode.bgt:
            case ClrOpcode.bge:
            case ClrOpcode.blt_un:
            case ClrOpcode.ble_un:
            case ClrOpcode.bgt_un:
            case ClrOpcode.bge_un:
            case ClrOpcode.leave:
            {
                var delta = buffer.read_i32_le();
                offset += 4;
                return new ClrBranchTarget32Operand { offset = (int)offset + delta };
            }

            case ClrOpcode.@switch:
            {
                var count = buffer.read_u32_le();
                offset += 4;
                var targets = new int[count];

                for (var i = 0; i < count; i++)
                {
                    targets[i] = (int)offset + (int)count * 4 + buffer.read_i32_le();
                    offset += 4;
                }

                return new ClrSwitchTargetsOperand { offsets = targets };
            }

            case ClrOpcode.call:
            case ClrOpcode.callvirt:
            case ClrOpcode.newobj:
            case ClrOpcode.ldstr:
            case ClrOpcode.ldftn:
            case ClrOpcode.ldvirtftn:
            case ClrOpcode.ldtoken:
            case ClrOpcode.castclass:
            case ClrOpcode.isinst:
            case ClrOpcode.unbox:
            case ClrOpcode.unbox_any:
            case ClrOpcode.box:
            case ClrOpcode.newarr:
            case ClrOpcode.ldelema:
            case ClrOpcode.ldelem_any:
            case ClrOpcode.stelem_any:
            case ClrOpcode.initobj:
            case ClrOpcode.constrained:
            case ClrOpcode.jmp:
            case ClrOpcode.calli:
            case ClrOpcode.cpobj:
            case ClrOpcode.ldobj:
            case ClrOpcode.stobj:
            case ClrOpcode.ldfld:
            case ClrOpcode.ldflda:
            case ClrOpcode.stfld:
            case ClrOpcode.ldsfld:
            case ClrOpcode.ldsflda:
            case ClrOpcode.stsfld:
            case ClrOpcode.@sizeof:
            case ClrOpcode.mkrefany:
            case ClrOpcode.refanyval:
            case ClrOpcode.refanytype:
            {
                var token = buffer.read_u32_le();
                offset += 4;
                return new ClrTokenOperand { value = token };
            }

            case ClrOpcode.unaligned:
            {
                var val = buffer.read_u8();
                offset += 1;
                return new ClrInt8Operand { value = (sbyte)val };
            }

            default:
                return null;
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     在元数据中查找指定类型的表的
    /// </summary>
    private static ClrTableData? find_table(ClrMetadata metadata, ClrTableKind kind)
    {
        if (metadata.table_stream == null) return null;

        foreach (var table in metadata.table_stream.tables)
            if (table.kind == kind)
                return table;

        return null;
    }

    /// <summary>
    ///     读取堆索引（2 的4 字节）的
    /// </summary>
    private static uint read_index(ref ByteBuffer buffer, int size)
    {
        return size == 4 ? buffer.read_u32_le() : buffer.read_u16_le();
    }

    /// <summary>
    ///     读取表索引（2 的4 字节）的
    /// </summary>
    private uint read_table_index(ref ByteBuffer buffer, ClrTableKind table, ClrTableHeader header)
    {
        return get_table_index_size(table, header) == 4 ? buffer.read_u32_le() : buffer.read_u16_le();
    }

    /// <summary>
    ///     读取编码索引的 的4 字节）的
    /// </summary>
    private uint read_coded_index(ref ByteBuffer buffer, ClrCodedIndex codedIndex, ClrTableHeader header)
    {
        return get_coded_index_size(codedIndex, header) == 4 ? buffer.read_u32_le() : buffer.read_u16_le();
    }

    /// <summary>
    ///     将偏移量对齐的4 字节边界的
    /// </summary>
    private static void align_to4(ref int offset)
    {
        offset = (offset + 3) & ~3;
    }

    #endregion

    #region 内部类型

    /// <summary>
    ///     CLR 编码索引类型（ECMA-335 §23.2.8）的
    /// </summary>
    private enum ClrCodedIndex
    {
        type_def_or_ref,
        has_constant,
        has_custom_attribute,
        has_field_marshal,
        has_decl_security,
        member_ref_parent,
        has_semantics,
        method_def_or_ref,
        member_forwarded,
        implementation,
        custom_attribute_type,
        resolution_scope,
        type_or_method_def
    }

    /// <summary>
    ///     方法体解码中间数据的
    /// </summary>
    private sealed class MethodBodyData
    {
        public uint code_size { get; init; }
        public ushort max_stack { get; init; }
        public uint local_var_sig_tok { get; init; }
        public byte[] code { get; init; } = [];
        public IReadOnlyList<ClrExceptionHandler> exception_handlers { get; init; } = [];
    }

    #endregion
}