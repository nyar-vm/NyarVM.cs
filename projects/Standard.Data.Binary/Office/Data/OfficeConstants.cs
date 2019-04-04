namespace Std.Data.Binary.Office.Data;

/// <summary>
///     Office 97-2003 二进制格式常量的
/// </summary>
public static class OfficeConstants
{
    /// <summary>
    ///     OLE2 Compound Document 魔数的
    /// </summary>
    /// <remarks>
    ///     XLS、DOC、PPT 的Office 97-2003 文件均使用此魔数的
    /// </remarks>
    public static ReadOnlySpan<byte> ole2_magic_number => [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    /// <summary>
    ///     XLS BIFF 记录类型的
    /// </summary>
    public static class XlsRecordType
    {
        /// <summary>
        ///     Beginning of File（BIFF2-7）的
        /// </summary>
        public const ushort bof = 0x0009;


        /// <summary>
        ///     Beginning of File（BIFF8 Workbook）的
        /// </summary>
        public const ushort bof8 = 0x0809;


        /// <summary>
        ///     文件类型 的工作簿全局（BIFF BOFTYPE）的
        /// </summary>
        public const ushort bof_type_workbook = 0x0005;


        /// <summary>
        ///     End of File的
        /// </summary>
        public const ushort eof = 0x000A;


        /// <summary>
        ///     公式的
        /// </summary>
        public const ushort formula = 0x0006;


        /// <summary>
        ///     标签/名称的
        /// </summary>
        public const ushort label = 0x0018;


        /// <summary>
        ///     数字的
        /// </summary>
        public const ushort number = 0x0203;


        /// <summary>
        ///     字符串标签的
        /// </summary>
        public const ushort label_sst = 0x00FD;


        /// <summary>
        ///     行的
        /// </summary>
        public const ushort row = 0x0208;


        /// <summary>
        ///     RK 值（紧凑数字）的
        /// </summary>
        public const ushort rk = 0x027E;


        /// <summary>
        ///     工作表绑定记录的
        /// </summary>
        public const ushort bound_sheet = 0x0085;


        /// <summary>
        ///     共享字符串表的
        /// </summary>
        public const ushort sst = 0x00FC;


        /// <summary>
        ///     工作的BOF（BIFF8 Workbook）的
        /// </summary>
        public const ushort bof_workbook = 0x0809;


        /// <summary>
        ///     BIFF 版本的
        /// </summary>
        public const ushort biff_version = 0x0600;


        /// <summary>
        ///     构建年份的
        /// </summary>
        public const ushort build_year = 0x09CD;


        /// <summary>
        ///     构建标识符的
        /// </summary>
        public const ushort build_identifier = 0x07C9;


        /// <summary>
        ///     写入访问记录的
        /// </summary>
        public const ushort write_access = 0x005C;


        /// <summary>
        ///     代码页记录的
        /// </summary>
        public const ushort code_page = 0x0042;


        /// <summary>
        ///     UTF-16LE 代码页值的
        /// </summary>
        public const ushort code_page_utf16_le = 0x04E4;


        /// <summary>
        ///     双精度存储文件记录的
        /// </summary>
        public const ushort dsf = 0x0161;


        /// <summary>
        ///     工作表状的的可见的
        /// </summary>
        public const byte sheet_state_visible = 0x01;
    }

    /// <summary>
    ///     Word DOC 流类型的
    /// </summary>
    public static class WordStreamType
    {
        /// <summary>
        ///     Grpprl 属性列表的
        /// </summary>
        public const byte grpprl = 0x01;


        /// <summary>
        ///     PieceTable 片段表的
        /// </summary>
        public const byte piece_table = 0x02;
    }

    /// <summary>
    ///     Word DOC 格式常量的
    /// </summary>
    public static class WordDoc
    {
        /// <summary>
        ///     Word 二进制文件标识符（wIdent）的
        /// </summary>
        public const ushort file_magic = 0xA5EC;


        /// <summary>
        ///     CLX 偏移量在文件中的位置的
        /// </summary>
        public const uint clx_offset_position = 0x00A2;
    }

    /// <summary>
    ///     PPT 记录类型的
    /// </summary>
    public static class PptRecordType
    {
        /// <summary>
        ///     幻灯片记录的
        /// </summary>
        public const ushort slide = 0x03EE;


        /// <summary>
        ///     文本字符记录的
        /// </summary>
        public const ushort text_chars_atom = 0x0FBA;


        /// <summary>
        ///     文本字节记录的
        /// </summary>
        public const ushort text_bytes_atom = 0x0FBC;
    }
}