namespace Legion.CLI.Compiler;

/// <summary>
///     包图解析结果。
/// </summary>
public sealed class PackageGraphResult
{
    public PackageGraphResult(bool success, List<string> files, List<string> errors)
    {
        this.success = success;
        this.files = [.. files];
        this.errors = errors;
    }

    /// <summary>
    ///     是否成功（有错误不影响成功，错误为警告级）
    /// </summary>
    public bool success { get; }

    /// <summary>
    ///     源码文件路径数组
    /// </summary>
    public string[] files { get; }

    /// <summary>
    ///     警告 / 错误信息
    /// </summary>
    public List<string> errors { get; }
}