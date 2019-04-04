using Std.Config.Node;

namespace Std.Config.Provider;

/// <summary>
///     字段级回退组合子，对每个字段优先从前面的源获取，缺失时回退到后面的源�?///
/// </summary>
public sealed class FallbackConfigProvider : IConfigProvider
{
    private readonly IConfigProvider[] _providers;

    /// <summary>
    ///     使用按优先级排列的配置源数组初始�?<see cref="FallbackConfigProvider" /> 的新实例�?    ///
    /// </summary>
    /// <param name="providers">按优先级排列的配置源数组�?/param>
    public FallbackConfigProvider(params IConfigProvider[] providers)
    {
        _providers = providers;
    }

    /// <summary>
    ///     加载并返回合并后的配置树根节点�?    /// 对每个字段，优先使用前面配置源的值，缺失时回退到后面的配置源�?    ///
    /// </summary>
    /// <returns>合并后的配置树根节点�?/returns>
    public ConfigNode load()
    {
        var mergedFields = new Dictionary<string, ConfigNode>();

        for (var i = _providers.Length - 1; i >= 0; i--)
        {
            var root = _providers[i].load();
            if (root is ObjectConfigNode obj) merge_into(mergedFields, obj);
        }

        return new ObjectConfigNode(mergedFields);
    }

    #region 合并逻辑

    private static void merge_into(Dictionary<string, ConfigNode> target, ObjectConfigNode source)
    {
        foreach (var kvp in source.enumerate_fields())
        {
            if (!target.TryGetValue(kvp.Key, out var existing))
            {
                target[kvp.Key] = kvp.Value;
                continue;
            }

            if (existing is ObjectConfigNode existingObj && kvp.Value is ObjectConfigNode newObj)
            {
                var innerFields = new Dictionary<string, ConfigNode>();
                foreach (var field in existingObj.enumerate_fields()) innerFields[field.Key] = field.Value;
                merge_into(innerFields, newObj);
                target[kvp.Key] = new ObjectConfigNode(innerFields);
            }
            else
            {
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    #endregion
}