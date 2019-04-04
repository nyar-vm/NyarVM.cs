namespace Std.Data.Binary.Gif.Data;

/// <summary>
///     GIF 图像格式常量
/// </summary>
public static class GifConstants
{
    /// <summary>
    ///     签名长度
    /// </summary>
    public const int signature_length = 6;


    /// <summary>
    ///     逻辑屏幕描述符大的
    /// </summary>
    public const int logical_screen_descriptor_size = 7;


    /// <summary>
    ///     图像描述符大的
    /// </summary>
    public const int image_descriptor_size = 10;


    /// <summary>
    ///     最大调色板大小
    /// </summary>
    public const int max_palette_size = 256;


    /// <summary>
    ///     LZW 最小码大小下限
    /// </summary>
    public const int min_lzw_code_size = 2;


    /// <summary>
    ///     LZW 最大码大小上限
    /// </summary>
    public const int max_lzw_code_size = 12;


    /// <summary>
    ///     块终止符
    /// </summary>
    public const byte block_terminator = 0x00;


    /// <summary>
    ///     图像分隔的
    /// </summary>
    public const byte image_separator = 0x2C;


    /// <summary>
    ///     扩展引入的
    /// </summary>
    public const byte extension_introducer = 0x21;


    /// <summary>
    ///     尾部标记
    /// </summary>
    public const byte trailer = 0x3B;


    /// <summary>
    ///     图形控制扩展标签
    /// </summary>
    public const byte graphic_control_label = 0xF9;


    /// <summary>
    ///     应用扩展标签
    /// </summary>
    public const byte application_extension_label = 0xFF;


    /// <summary>
    ///     注释扩展标签
    /// </summary>
    public const byte comment_extension_label = 0xFE;


    /// <summary>
    ///     纯文本扩展标的
    /// </summary>
    public const byte plain_text_label = 0x01;

    /// <summary>
    ///     GIF87a 签名
    /// </summary>
    public static ReadOnlySpan<byte> signature87_a => "GIF87a"u8;


    /// <summary>
    ///     GIF89a 签名
    /// </summary>
    public static ReadOnlySpan<byte> signature89_a => "GIF89a"u8;


    /// <summary>
    ///     Netscape 应用标识的
    /// </summary>
    public static ReadOnlySpan<byte> netscape_app_id => "NETSCAPE2.0"u8;


    /// <summary>
    ///     计算调色板大小（字节数）
    /// </summary>
    /// <param name="packedField">逻辑屏幕描述符的打包字段。</param>
    /// <returns>调色板字节数。</returns>
    public static int global_color_table_size(byte packedField)
    {
        var hasGct = (packedField & 0x80) != 0;
        if (!hasGct) return 0;

        var sizeField = packedField & 0x07;
        return 3 * (1 << (sizeField + 1));
    }


    /// <summary>
    ///     计算局部调色板大小（字节数的
    /// </summary>
    /// <param name="packedField">图像描述符的打包字段。</param>
    /// <returns>调色板字节数。</returns>
    public static int local_color_table_size(byte packedField)
    {
        var hasLct = (packedField & 0x80) != 0;
        if (!hasLct) return 0;

        var sizeField = packedField & 0x07;
        return 3 * (1 << (sizeField + 1));
    }
}