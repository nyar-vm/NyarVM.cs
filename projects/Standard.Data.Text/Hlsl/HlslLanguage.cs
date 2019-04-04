namespace Std.Data.Text.Hlsl;


/// <summary>

///     HLSL 语言配置。


/// </summary>
public sealed class HlslLanguage : Oak.Syntax.Language
{
    
/// <summary>
    
///     语言名称。
    

/// </summary>
    public override string Name => "HLSL";

    
/// <summary>
    
///     着色器模型版本（如 50 表示 5.0）。
    

/// </summary>
    public int ShaderModel { get; init; } = 50;

    
/// <summary>
    
///     是否启用 DirectX 12 特性。
    

/// </summary>
    public bool Dx12Features { get; init; }

    
/// <summary>
    
///     是否启用光线追踪。
    

/// </summary>
    public bool RayTracing { get; init; }

    
/// <summary>
    
///     是否启用 Mesh Shader。
    

/// </summary>
    public bool MeshShader { get; init; }

    
/// <summary>
    
///     是否启用 Amplification Shader。
    

/// </summary>
    public bool AmplificationShader { get; init; }
}
