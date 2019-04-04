using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie;

/// <summary>
///     Valkyrie 语言配置
/// </summary>
public sealed class ValkyrieLanguage : Language
{
    public ValkyrieLanguage()
    {
        name = "Valkyrie";
    }

    private ValkyrieLanguage(string name)
    {
        this.name = name;
    }

    /// <summary>
    ///     语言名称
    /// </summary>
    public override string name { get; }

    /// <summary>
    ///     文件扩展名列表
    /// </summary>
    public IReadOnlyList<string> extensions { get; init; } = [".v", ".script"];

    /// <summary>
    ///     是否支持 Schema 扩展（Hermes 模式）
    ///     启用后支持 storage, service, rpc 等声明
    /// </summary>
    public bool support_schema_extension { get; init; }

    /// <summary>
    ///     是否支持 ECS 扩展
    ///     启用后支持 entity, component, system 等声明
    /// </summary>
    public bool support_ecs_extension { get; init; } = true;

    /// <summary>
    ///     是否支持 UI 扩展
    /// </summary>
    public bool support_ui_extension { get; init; } = true;

    /// <summary>
    ///     是否支持 Shader 扩展
    ///     启用后支持 shader, uniform, varying, texture, sampler 等着色器语句
    /// </summary>
    public bool support_shader_extension { get; init; }

    /// <summary>
    ///     标准 Valkyrie 语言（游戏脚本模式）
    /// </summary>
    public static ValkyrieLanguage standard => new("Valkyrie")
    {
        extensions = [".script"],
        support_schema_extension = false,
        support_ecs_extension = true,
        support_ui_extension = true,
        support_shader_extension = false
    };

    /// <summary>
    ///     Editor UI 模式（Widget SFC 模式）
    /// </summary>
    public static ValkyrieLanguage editor_ui => new("Valkyrie Editor UI")
    {
        extensions = [".widget"],
        support_schema_extension = false,
        support_ecs_extension = false,
        support_ui_extension = true,
        support_shader_extension = false
    };

    /// <summary>
    ///     Schema 模式（数据契约模式）
    /// </summary>
    public static ValkyrieLanguage schema => new("Valkyrie Schema")
    {
        extensions = [".schema"],
        support_schema_extension = true,
        support_ecs_extension = false,
        support_ui_extension = false,
        support_shader_extension = false
    };

    /// <summary>
    ///     Shader 模式（着色器语言模式）
    /// </summary>
    public static ValkyrieLanguage shader => new("Valkyrie Shader")
    {
        extensions = [".shader"],
        support_schema_extension = false,
        support_ecs_extension = false,
        support_ui_extension = false,
        support_shader_extension = true
    };
}