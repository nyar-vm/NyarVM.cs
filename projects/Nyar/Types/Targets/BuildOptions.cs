namespace Nyar.Types.Targets;

/// <summary>
///     下游构建附加选项，与 <see cref="CanonicalTarget" /> 组合构成完整 <c>BuildTarget</c>。
/// </summary>
/// <remarks>
///     <para>设计原则：</para>
///     <list type="number">
///         <item><see cref="CanonicalTarget" /> 只回答"编译给谁执行"</item>
///         <item><c>BuildOptions</c> 回答"如何交付"——宿主协议、打包格式、分发渠道</item>
///         <item>此类型属于下游构建概念，不属于 Nyar 核心目标抽象</item>
///     </list>
/// </remarks>
public sealed record BuildOptions
{
    /// <summary>
    ///     编译输出细节选项
    /// </summary>
    public BuildEmitOptions emit { get; init; } = new();

    /// <summary>
    ///     宿主方言与宿主协议选项
    /// </summary>
    public BuildHostOptions host { get; init; } = new();

    /// <summary>
    ///     打包格式选项
    /// </summary>
    public BuildPackageOptions package { get; init; } = new();

    /// <summary>
    ///     分发渠道选项
    /// </summary>
    public BuildChannelOptions channel { get; init; } = new();

    /// <summary>
    ///     资源布局与静态资源选项
    /// </summary>
    public BuildAssetOptions assets { get; init; } = new();

    /// <summary>
    ///     签名与证书选项
    /// </summary>
    public BuildSigningOptions signing { get; init; } = new();

    /// <summary>
    ///     目标宿主所需清单与元数据生成选项
    /// </summary>
    public BuildManifestOptions manifest { get; init; } = new();
}

/// <summary>
///     编译输出细节选项。
///     控制编译器产物中额外生成的辅助文件。
/// </summary>
public sealed record BuildEmitOptions
{
    /// <summary>
    ///     是否生成 Source Map
    /// </summary>
    public bool source_map { get; init; }

    /// <summary>
    ///     是否生成 TypeScript 声明文件（.d.ts）
    /// </summary>
    public bool type_script { get; init; }

    /// <summary>
    ///     是否生成 WAT（WebAssembly Text Format）文本输出
    /// </summary>
    public bool wat { get; init; }

    /// <summary>
    ///     是否生成 MSIL 文本输出
    /// </summary>
    public bool msil { get; init; }
}

/// <summary>
///     宿主方言与宿主协议选项。
///     描述代码最终运行在哪类宿主环境以及对应的协议约束。
/// </summary>
public sealed record BuildHostOptions
{
    /// <summary>
    ///     宿主风味标识。
    ///     例如 <c>browser</c>、<c>node</c>、<c>extension-host</c>、<c>mini-game</c>。
    /// </summary>
    public string flavor { get; init; } = "default";

    /// <summary>
    ///     宿主能力标签。
    ///     描述宿主提供的 API 面，如 <c>dom</c>、<c>filesystem</c>、<c>lifecycle</c>、<c>bridge-api</c>。
    /// </summary>
    public string[] capabilities { get; init; } = [];

    /// <summary>
    ///     模块系统类型。
    ///     例如 <c>esmodule</c>、<c>commonjs</c>、<c>amd</c>、<c>system</c>。
    /// </summary>
    public string module_system { get; init; } = "esmodule";
}

/// <summary>
///     打包格式选项。
///     定义最终交付产物的打包格式。
/// </summary>
public sealed record BuildPackageOptions
{
    /// <summary>
    ///     打包格式标识。
    ///     例如 <c>directory</c>、<c>web-app</c>、<c>extension</c>、<c>app-package</c>、<c>mini-game-package</c>。
    /// </summary>
    public string format { get; init; } = "directory";

    /// <summary>
    ///     是否启用分包（小程序/小游戏的 subpackage 机制）
    /// </summary>
    public bool enable_sub_package { get; init; }

    /// <summary>
    ///     额外的打包配置参数，由具体 packager 解释
    /// </summary>
    public Dictionary<string, string> extra { get; init; } = new();
}

/// <summary>
///     分发渠道选项。
///     定义产物最终的分发渠道和上架规则。
/// </summary>
public sealed record BuildChannelOptions
{
    /// <summary>
    ///     渠道类型标识。
    ///     例如 <c>self-hosted</c>、<c>marketplace</c>、<c>open-platform</c>。
    /// </summary>
    public string kind { get; init; } = "self-hosted";

    /// <summary>
    ///     渠道专有配置参数，由具体 publisher 解释
    /// </summary>
    public Dictionary<string, string> extra { get; init; } = new();
}

/// <summary>
///     资源布局选项。
///     控制静态资源的组织方式与加载策略。
/// </summary>
public sealed record BuildAssetOptions
{
    /// <summary>
    ///     资源根路径
    /// </summary>
    public string root { get; init; } = "assets";

    /// <summary>
    ///     资源打包策略，例如 <c>inline</c>、<c>bundle</c>、<c>external</c>
    /// </summary>
    public string strategy { get; init; } = "bundle";
}

/// <summary>
///     签名与证书选项。
///     定义产物签名、证书和 provisioning 要求。
/// </summary>
public sealed record BuildSigningOptions
{
    /// <summary>
    ///     是否启用代码签名
    /// </summary>
    public bool enabled { get; init; }

    /// <summary>
    ///     签名证书标识或路径
    /// </summary>
    public string? certificate { get; init; }

    /// <summary>
    ///     Provisioning Profile 路径（iOS/macOS）
    /// </summary>
    public string? provisioning_profile { get; init; }
}

/// <summary>
///     清单与元数据生成选项。
///     控制目标宿主所需清单文件的生成规则。
/// </summary>
public sealed record BuildManifestOptions
{
    /// <summary>
    ///     清单文件格式，例如 <c>package.json</c>、<c>manifest.json</c>、<c>plugin.xml</c>
    /// </summary>
    public string format { get; init; } = "none";

    /// <summary>
    ///     清单中需要注入的元数据键值对
    /// </summary>
    public Dictionary<string, string> fields { get; init; } = new();
}