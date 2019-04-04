using System.Text;
using Std.Data.Binary.Dwarf.Decode;

namespace Std.Data.Binary.Dwarf.Scanner;

/// <summary>
///     DWARF 文件扫描器，提供对调试信息的快速结构扫描的
/// </summary>
public class DwarfScanner
{
    /// <summary>
    ///     扫描 DWARF 文件，提取结构信息的
    /// </summary>
    /// <param name="data">
    ///     DWARF 二进制数据的/param>
    ///     <returns>扫描结果的/returns>
    public static string scan(byte[] data)
    {
        var result = new StringBuilder();
        result.AppendLine("DWARF File Scan Result:");
        result.AppendLine("======================");

        if (data.Length < 11)
        {
            result.AppendLine("的文件数据过短");
            return result.ToString();
        }

        result.AppendLine("的有效的DWARF 数据");
        result.AppendLine();

        try
        {
            var decoder = new DwarfDecoder();
            var dwarfFile = decoder.decode(data);

            result.AppendLine($"📋 编译单元数量: {dwarfFile.compilation_units.Count}");

            if (dwarfFile.compilation_units.Count > 0)
            {
                result.AppendLine();
                result.AppendLine("编译单元列表:");
                result.AppendLine("- - - - - - - - - - - - - - - - - - - -");

                foreach (var unit in dwarfFile.compilation_units)
                {
                    result.AppendLine($"  📄 DWARF {unit.version}");
                    result.AppendLine($"     单元长度: {unit.unit_length} bytes");
                    result.AppendLine($"     地址大小: {unit.address_size} bytes");
                    result.AppendLine($"     条目数量: {unit.entries.Count}");

                    if (unit.entries.Count > 0)
                    {
                        result.AppendLine("     标签类型:");
                        foreach (var entry in unit.entries.Take(5))
                            result.AppendLine($"       - {get_tag_name(entry.tag)}");
                        if (unit.entries.Count > 5) result.AppendLine($"       ... 还有 {unit.entries.Count - 5} 个条目");
                    }
                }
            }

            result.AppendLine();

            result.AppendLine($"📊 行号表数的 {dwarfFile.line_number_tables.Count}");
        }
        catch (Exception ex)
        {
            result.AppendLine($"⚠️ 扫描过程中出现错的 {ex.Message}");
        }

        return result.ToString();
    }


    /// <summary>
    ///     获取标签名称的
    /// </summary>
    private static string get_tag_name(uint tag)
    {
        return tag switch
        {
            0x01 => "DW_TAG_array_type",
            0x02 => "DW_TAG_class_type",
            0x03 => "DW_TAG_entry_point",
            0x04 => "DW_TAG_enumeration_type",
            0x05 => "DW_TAG_formal_parameter",
            0x08 => "DW_TAG_imported_declaration",
            0x0A => "DW_TAG_label",
            0x0B => "DW_TAG_lexical_block",
            0x0D => "DW_TAG_member",
            0x0F => "DW_TAG_pointer_type",
            0x10 => "DW_TAG_reference_type",
            0x11 => "DW_TAG_compile_unit",
            0x12 => "DW_TAG_string_type",
            0x13 => "DW_TAG_structure_type",
            0x15 => "DW_TAG_subroutine_type",
            0x16 => "DW_TAG_typedef",
            0x17 => "DW_TAG_union_type",
            0x18 => "DW_TAG_unspecified_parameters",
            0x19 => "DW_TAG_variant",
            0x1A => "DW_TAG_common_block",
            0x1B => "DW_TAG_common_inclusion",
            0x1C => "DW_TAG_inheritance",
            0x1D => "DW_TAG_inlined_subroutine",
            0x1E => "DW_TAG_module",
            0x1F => "DW_TAG_ptr_to_member_type",
            0x20 => "DW_TAG_set_type",
            0x21 => "DW_TAG_subrange_type",
            0x22 => "DW_TAG_with_stmt",
            0x23 => "DW_TAG_access_declaration",
            0x24 => "DW_TAG_base_type",
            0x25 => "DW_TAG_catch_block",
            0x26 => "DW_TAG_const_type",
            0x27 => "DW_TAG_constant",
            0x28 => "DW_TAG_enumerator",
            0x29 => "DW_TAG_file_type",
            0x2A => "DW_TAG_friend",
            0x2B => "DW_TAG_namelist",
            0x2C => "DW_TAG_namelist_item",
            0x2D => "DW_TAG_packed_type",
            0x2E => "DW_TAG_subprogram",
            0x2F => "DW_TAG_template_type_parameter",
            0x30 => "DW_TAG_template_value_parameter",
            0x31 => "DW_TAG_thrown_type",
            0x32 => "DW_TAG_try_block",
            0x33 => "DW_TAG_variant_part",
            0x34 => "DW_TAG_variable",
            0x35 => "DW_TAG_volatile_type",
            0x36 => "DW_TAG_dwarf_procedure",
            0x37 => "DW_TAG_restrict_type",
            0x38 => "DW_TAG_interface_type",
            0x39 => "DW_TAG_namespace",
            0x3A => "DW_TAG_imported_module",
            0x3B => "DW_TAG_unspecified_type",
            0x3C => "DW_TAG_partial_unit",
            0x3D => "DW_TAG_imported_unit",
            0x3F => "DW_TAG_condition",
            0x40 => "DW_TAG_shared_type",
            0x41 => "DW_TAG_type_unit",
            0x42 => "DW_TAG_rvalue_reference_type",
            0x43 => "DW_TAG_template_alias",
            _ => $"未知标签 (0x{tag:X4})"
        };
    }
}