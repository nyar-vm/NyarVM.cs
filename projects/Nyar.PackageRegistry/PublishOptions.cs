namespace Nyar.PackageRegistry;

/// <summary>
///     发布选项
/// </summary>
public class PublishOptions
{
    /// <summary>
    ///     包名称
    /// </summary>
    public string package_name { get; set; } = string.Empty;

    /// <summary>
    ///     版本号
    /// </summary>
    public string version { get; set; } = string.Empty;

    /// <summary>
    ///     包描述
    /// </summary>
    public string description { get; set; } = string.Empty;

    /// <summary>
    ///     项目主页
    /// </summary>
    public string? homepage { get; set; }

    /// <summary>
    ///     许可证
    /// </summary>
    public string? license { get; set; }

    /// <summary>
    ///     包本地路径
    /// </summary>
    public string package_path { get; set; } = string.Empty;

    /// <summary>
    ///     目标注册表名称
    /// </summary>
    public string registry_name { get; set; } = "npm";

    /// <summary>
    ///     认证令牌
    /// </summary>
    public string? auth_token { get; set; }

    /// <summary>
    ///     发布标签
    /// </summary>
    public string? tag { get; set; }

    /// <summary>
    ///     访问级别（public/restricted）
    /// </summary>
    public string? access { get; set; }

    /// <summary>
    ///     版本递增类型（优先级高于手动指定版本）
    /// </summary>
    public VersionBump? bump { get; set; }

    /// <summary>
    ///     是否自动创建 Git Tag
    /// </summary>
    public bool create_git_tag { get; set; } = true;

    /// <summary>
    ///     Git Tag 前缀（如 "v"，最终 tag 为 "v1.0.0"）
    /// </summary>
    public string? git_tag_prefix { get; set; } = "v";

    /// <summary>
    ///     跳过 Git 工作区清洁检查
    /// </summary>
    public bool skip_git_check { get; set; }

    /// <summary>
    ///     发布前执行 prePublish 脚本
    /// </summary>
    public bool run_pre_publish_script { get; set; } = true;
}