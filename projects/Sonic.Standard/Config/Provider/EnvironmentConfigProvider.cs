using System.Collections;
using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     基于环境变量的配置源，将带前缀的环境变量转换为层级化的 ConfigNode 树�?/// 使用双下划线 __ 作为嵌套分隔符，单下划线 _ 作为同级分隔符�?///
/// </summary>
public sealed class EnvironmentConfigProvider : IConfigProvider
{
    private readonly string _prefix;

    /// <summary>
    ///     使用指定的环境变量前缀初始�?<see cref="EnvironmentConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="prefix">环境变量前缀，默认为 "APP_"�?/param>
    public EnvironmentConfigProvider(string prefix = "APP_")
    {
        _prefix = prefix;
    }

    /// <summary>
    ///     加载并返回配置树根节点�?    ///
    /// </summary>
    /// <returns>配置树的根节点�?/returns>
    public ConfigNode load()
    {
        var builder = new ConfigNodeBuilder();
        var variables = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry entry in variables)
        {
            var key = entry.Key?.ToString();
            var value = entry.Value?.ToString();

            if (key is null || value is null || !key.StartsWith(_prefix)) continue;

            var path = key[_prefix.Length..];
            path = path.Replace("__", ".").Replace("_", ".").ToLowerInvariant();
            builder.add(path, value);
        }

        return builder.build();
    }
}