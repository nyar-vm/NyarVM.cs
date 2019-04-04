using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Legion.CLI.Compiler;

/// <summary>
///     Legion 构建器存根。
///     提供 <c>build</c>、<c>build_in_memory</c>、<c>clean</c> 的公共 API，
///     其中 <c>build</c> 和 <c>build_in_memory</c> 需要 <c>ValkyrieCompiler</c>，
///     待 <c>ValkyrieCompiler</c> 重新纳入 <c>Nyar.Language</c> 编译后恢复完整实现。
/// </summary>
public sealed class LegionCompiler
{
    /// <summary>
    ///     创建构建器实例
    /// </summary>
    /// <param name="cache">增量编译缓存，为 null 则执行全量编译</param>
    public LegionCompiler(ICompilationCache? cache = null)
    {
    }

    /// <summary>
    ///     构建项目并将产物写入磁盘。
    ///     当前为存根实现，待 ValkyrieCompiler 可用后恢复。
    /// </summary>
    /// <param name="context">编译上下文</param>
    /// <returns>构建结果</returns>
    public LegionBuildResult build(CompilationContext context)
    {
        throw new NotImplementedException(
            "LegionCompiler.build 需要 ValkyrieCompiler，" +
            "待 Nyar.Language 重新纳入 ValkyrieCompiler 编译后恢复");
    }

    /// <summary>
    ///     构建项目并返回内存中的产物（不落盘），供 run 命令直接交给 NyarVM 执行。
    ///     当前为存根实现，待 ValkyrieCompiler 可用后恢复。
    /// </summary>
    /// <param name="context">编译上下文</param>
    /// <returns>构建结果</returns>
    public LegionBuildResult build_in_memory(CompilationContext context)
    {
        throw new NotImplementedException(
            "LegionCompiler.build_in_memory 需要 ValkyrieCompiler，" +
            "待 Nyar.Language 重新纳入 ValkyrieCompiler 编译后恢复");
    }

    /// <summary>
    ///     清理项目的构建产物
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    public void clean(string projectDir)
    {
        var distDir = Path.Combine(projectDir, "dist");
        if (Directory.Exists(distDir))
        {
            Directory.Delete(distDir, true);
        }
    }
}
