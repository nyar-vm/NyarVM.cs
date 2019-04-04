using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     基于命令行参数的配置源，支持 --key=value�?-key value�?-no-flag�?-flag 等格式�?/// 使用冒号 : 作为嵌套路径分隔符�?///
/// </summary>
public sealed class CliConfigProvider : IConfigProvider
{
    private readonly string[] _args;

    /// <summary>
    ///     使用指定的命令行参数数组初始�?<see cref="CliConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="args">命令行参数数组�?/param>
    public CliConfigProvider(string[] args)
    {
        _args = args;
    }

    /// <summary>
    ///     加载并返回配置树根节点�?    ///
    /// </summary>
    /// <returns>配置树的根节点�?/returns>
    public ConfigNode load()
    {
        var builder = new ConfigNodeBuilder();

        for (var i = 0; i < _args.Length; i++)
        {
            var arg = _args[i];

            if (!arg.StartsWith("--")) continue;

            var body = arg[2..];

            if (body.StartsWith("no-"))
            {
                var key = body[3..].Replace(':', '.').ToLowerInvariant();
                builder.add(key, "false");
                continue;
            }

            var eqIndex = body.IndexOf('=');
            if (eqIndex >= 0)
            {
                var key = body[..eqIndex].Replace(':', '.').ToLowerInvariant();
                var value = body[(eqIndex + 1)..];
                builder.add(key, value);
                continue;
            }

            if (i + 1 < _args.Length && !_args[i + 1].StartsWith("--"))
            {
                var key = body.Replace(':', '.').ToLowerInvariant();
                var value = _args[i + 1];
                builder.add(key, value);
                i++;
                continue;
            }

            var flagKey = body.Replace(':', '.').ToLowerInvariant();
            builder.add(flagKey, "true");
        }

        return builder.build();
    }
}