namespace Nyar.PackageManager.Build;

/// <summary>
///     编译器产物
/// </summary>
public interface ICompilerArtifact
{
    /// <summary>
    ///     产物文件名
    /// </summary>
    string name { get; }

    /// <summary>
    ///     产物内容
    /// </summary>
    byte[] content { get; }

    /// <summary>
    ///     产物媒体类型
    /// </summary>
    string media_type { get; }
}