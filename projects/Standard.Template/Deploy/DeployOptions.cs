namespace Std.Template.Deploy;

/// <summary>
///     部署选项
/// </summary>
public class DeployOptions
{
    /// <summary>
    ///     部署目标类型：gh-pages 或 rsync
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    ///     Git 仓库地址（gh-pages 部署时使用）
    /// </summary>
    public string RepoUrl { get; set; } = string.Empty;

    /// <summary>
    ///     SSH 主机地址（rsync 部署时使用）
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    ///     远程目录路径（rsync 部署时使用）
    /// </summary>
    public string RemotePath { get; set; } = string.Empty;

    /// <summary>
    ///     部署分支名称，默认为 gh-pages
    /// </summary>
    public string Branch { get; set; } = "gh-pages";

    /// <summary>
    ///     提交信息
    /// </summary>
    public string Message { get; set; } = "Deploy site";
}