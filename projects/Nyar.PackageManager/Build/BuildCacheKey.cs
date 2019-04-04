using System.Security.Cryptography;
using System.Text;

namespace Nyar.PackageManager.Build;

/// <summary>
///     构建缓存键
/// </summary>
[Obsolete("请使用 Nyar.Language.Valkyrie.Compiler.Pipeline.NyarDatabaseCompilationCache 代替")]
public sealed record BuildCacheKey
{
    /// <summary>
    ///     模块名称
    /// </summary>
    public string module_name { get; init; } = string.Empty;

    /// <summary>
    ///     目标三元组
    /// </summary>
    public string canonical_triple { get; init; } = string.Empty;

    /// <summary>
    ///     源码哈希
    /// </summary>
    public string source_hash { get; init; } = string.Empty;

    /// <summary>
    ///     从构建计划与源码生成缓存键
    /// </summary>
    /// <param name="plan">构建计划</param>
    /// <param name="source">源码</param>
    /// <returns>构建缓存键</returns>
    public static BuildCacheKey from_plan(IBuildPlan plan, string source)
    {
        return from_plan(plan, [("<memory>", source)]);
    }

    /// <summary>
    ///     从构建计划与多文件源码生成缓存键。
    /// </summary>
    /// <param name="plan">构建计划</param>
    /// <param name="sourceFiles">源码文件列表</param>
    /// <returns>构建缓存键</returns>
    public static BuildCacheKey from_plan(IBuildPlan plan, IReadOnlyList<string> sourceFiles)
    {
        var inputs = sourceFiles
            .Select(file => (Path.GetFullPath(file), File.ReadAllText(file)))
            .ToArray();
        return from_plan(plan, inputs);
    }

    /// <summary>
    ///     从规范化源码输入生成缓存键。
    /// </summary>
    /// <param name="plan">构建计划</param>
    /// <param name="inputs">源码输入列表</param>
    /// <returns>构建缓存键</returns>
    private static BuildCacheKey from_plan(IBuildPlan plan, IReadOnlyList<(string Path, string Content)> inputs)
    {
        var builder = new StringBuilder();
        foreach (var input in inputs.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(input.Path);
            builder.Append('\n');
            builder.Append(input.Content);
            builder.Append("\n\0\n");
        }

        var sourceBytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hashBytes = SHA256.HashData(sourceBytes);
        var hash = Convert.ToHexStringLower(hashBytes);

        return new BuildCacheKey
        {
            module_name = plan.module_name,
            canonical_triple = plan.canonical_triple,
            source_hash = hash
        };
    }

    /// <summary>
    ///     生成本地缓存文件名
    /// </summary>
    /// <returns>文件名</returns>
    public string to_file_name()
    {
        return $"{module_name}_{canonical_triple.Replace('-', '_')}_{source_hash}.json";
    }
}