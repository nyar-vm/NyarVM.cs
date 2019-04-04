namespace Std.Data.Binary.Live2D.Scanner;

/// <summary>
///     Live2D 格式扫描器接口，提供的Live2D 二进制数据的快速探查能力的
/// </summary>
/// <remarks>
///     Live2D 的Live2D Inc. 开发的参数的2D 动画格式，广泛用于虚拟主播和游戏角色的
///     扫描器专注于快速识的Live2D 文件版本、参数数量、部件数量等元信息，
///     不做完整的对象反序列化，以实现零分配高性能扫描的
///     扫描器通过段偏移表定位 CountInfo 段，从中读取各类型元素数量的
/// </remarks>
public interface ILive2DScanner
{
    /// <summary>
    ///     扫描 moc3 文件头，提取版本和字节序信息的
    /// </summary>
    /// <returns>包含版本号、字节序标志和修订号的元组的/returns>
    (int Version, bool IsBigEndian, int Revision) scan_moc3_header();

    /// <summary>
    ///     扫描 moc3 文件，提取参数数量的
    /// </summary>
    /// <returns>参数数量的/returns>
    int scan_moc3_parameter_count();

    /// <summary>
    ///     扫描 moc3 文件，提取部件数量的
    /// </summary>
    /// <returns>部件数量的/returns>
    int scan_moc3_part_count();

    /// <summary>
    ///     扫描 moc3 文件，提取绘制对象数量的
    /// </summary>
    /// <returns>绘制对象数量的/returns>
    int scan_moc3_drawable_count();

    /// <summary>
    ///     扫描 moc3 文件，提取变形器数量（v4+）的
    /// </summary>
    /// <returns>变形器数量的/returns>
    int scan_moc3_deformer_count();

    /// <summary>
    ///     扫描 moc3 文件，提取纹理数量的
    /// </summary>
    /// <returns>纹理数量的/returns>
    int scan_moc3_texture_count();

    /// <summary>
    ///     扫描 model3.json 文件，提取文件引用列表的
    /// </summary>
    /// <returns>文件路径列表的/returns>
    List<string> scan_file_references();
}