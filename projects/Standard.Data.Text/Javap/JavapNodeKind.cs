using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     Javap 语法节点类型
/// </summary>
public static class JavapNodeKind
{
    public static readonly NodeKind unknown = 0;

    #region 顶层结构

    public static readonly NodeKind compiled_from_header = 1;
    public static readonly NodeKind class_declaration = 2;
    public static readonly NodeKind field_declaration = 3;
    public static readonly NodeKind method_declaration = 4;

    #endregion

    #region 方法体

    public static readonly NodeKind code_section = 10;
    public static readonly NodeKind instruction = 11;
    public static readonly NodeKind exception_table = 12;
    public static readonly NodeKind line_number_table = 13;
    public static readonly NodeKind stack_map_table = 14;

    #endregion

    #region Token 类型

    public static readonly NodeKind access_modifier = 20;
    public static readonly NodeKind type_keyword = 21;
    public static readonly NodeKind opcode = 22;
    public static readonly NodeKind identifier = 23;
    public static readonly NodeKind number = 24;
    public static readonly NodeKind constant_pool_ref = 25;
    public static readonly NodeKind comment = 26;
    public static readonly NodeKind punctuation = 27;
    public static readonly NodeKind eof = 28;

    #endregion
}