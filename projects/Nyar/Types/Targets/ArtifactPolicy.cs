namespace Nyar.Types.Targets;

/// <summary>
///     产物策略，定义编译产物的文件集合与布局规则。
/// </summary>
public sealed record ArtifactPolicy
{
    /// <summary>
    ///     主产物扩展名
    /// </summary>
    public string primary_extension { get; init; } = string.Empty;

    /// <summary>
    ///     默认发布格式，例如 directory / jar / bundle / extension / mini-game
    /// </summary>
    public string default_publish_format { get; init; } = "directory";

    /// <summary>
    ///     该目标可支持的发布格式列表。
    ///     发行版层可以在这些格式上叠加具体产品工作流，而无需修改目标模型。
    /// </summary>
    public string[] supported_publish_formats { get; init; } = [];

    /// <summary>
    ///     进入目标宿主前需要补齐的适配层。
    ///     例如 std:web / std:node / std:extension-host / std:mini-game / std:mobile。
    /// </summary>
    public string[] required_adaptors { get; init; } = [];

    /// <summary>
    ///     是否生成运行时配置文件
    /// </summary>
    public bool generate_runtime_config { get; init; }

    /// <summary>
    ///     是否生成调试符号
    /// </summary>
    public bool generate_debug_symbols { get; init; }

    /// <summary>
    ///     是否生成启动脚本
    /// </summary>
    public bool generate_launch_scripts { get; init; } = true;

    /// <summary>
    ///     是否生成 XML 文档
    /// </summary>
    public bool generate_xml_doc { get; init; }

    /// <summary>
    ///     对外分发时通常是否要求签名
    /// </summary>
    public bool requires_code_signing { get; init; }

    /// <summary>
    ///     是否天然对接商店/托管分发流程（如应用包格式、扩展包格式）
    /// </summary>
    public bool supports_store_distribution { get; init; }
}