namespace Nyar.PackageManager.Build;

/// <summary>
///     构建阶段
/// </summary>
public enum BuildStage
{
    /// <summary>词法分析</summary>
    lex,

    /// <summary>语法分析</summary>
    parse,

    /// <summary>语义分析</summary>
    semantic,

    /// <summary>HIR 构建</summary>
    hir,

    /// <summary>MIR 构建（EGraph + IKun）</summary>
    mir,

    /// <summary>LIR 构建（Nyar Standard IR）</summary>
    lir,

    /// <summary>目标发射（后端代码生成）</summary>
    emit,

    /// <summary>打包</summary>
    packaging,

    /// <summary>写入磁盘</summary>
    flush
}