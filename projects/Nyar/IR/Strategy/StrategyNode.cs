using System.Collections.Immutable;
using Nyar.IR.Intent;

namespace Nyar.IR.Strategy;

/// <summary>
///     策略层节点基类 - 表达"怎么做"的调度、布局和向量化决策
/// </summary>
public abstract record StrategyNode : AlgebraNode
{
    #region 上下文（约束）

    /// <summary>
    ///     带约束
    /// </summary>
    /// <param name="value">值。</param>
    /// <param name="constraint">约束。</param>
    public sealed record WithConstraint(Id value, Id constraint) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [value, constraint];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new WithConstraint(f(value), f(constraint));
        }
    }

    #endregion

    #region 扩展点

    /// <summary>
    ///     扩展操作
    /// </summary>
    /// <param name="name">扩展名称。</param>
    /// <param name="children">扩展子节点。</param>
    [Obsolete("请使用类型化的方言节点继承 Oa 并标注 [OaNode]，而非使用无类型的 Extension 节点")]
    public sealed record Extension(string name, ImmutableArray<Id> children) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return children;
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Extension(name, [.. children.Select(f)]);
        }
    }

    /// <summary>
    ///     跨语言调用
    /// </summary>
    /// <param name="source_lang">源语言。</param>
    /// <param name="target_lang">目标语言。</param>
    /// <param name="function">函数引用。</param>
    /// <param name="arguments">参数列表。</param>
    public sealed record CrossLangCall(
        string source_lang,
        string target_lang,
        Id function,
        ImmutableArray<Id> arguments)
        : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            var ids = new List<Id>(arguments.Length + 1) { function };
            ids.AddRange(arguments);
            return ids;
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new CrossLangCall(source_lang, target_lang, f(function), [.. arguments.Select(f)]);
        }
    }

    #endregion

    #region 上下文

    /// <summary>
    ///     带上下文
    /// </summary>
    /// <param name="value">值。</param>
    /// <param name="context">上下文。</param>
    public sealed record WithContext(Id value, Id context) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [value, context];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new WithContext(f(value), f(context));
        }
    }

    /// <summary>
    ///     CPU 上下文
    /// </summary>
    public sealed record CpuContext : StrategyNode;

    /// <summary>
    ///     GPU 上下文
    /// </summary>
    public sealed record GpuContext : StrategyNode;

    /// <summary>
    ///     异步上下文
    /// </summary>
    public sealed record AsyncContext : StrategyNode;

    /// <summary>
    ///     空间上下文
    /// </summary>
    public sealed record SpatialContext : StrategyNode;

    /// <summary>
    ///     编译期上下文
    /// </summary>
    public sealed record ComptimeContext : StrategyNode;

    /// <summary>
    ///     资源上下文
    /// </summary>
    public sealed record ResourceContext : StrategyNode;

    /// <summary>
    ///     安全上下文
    /// </summary>
    public sealed record SafeContext : StrategyNode;

    #endregion

    #region 约束

    /// <summary>
    ///     所有权约束
    /// </summary>
    /// <param name="ownership">所有权类型。</param>
    public sealed record OwnershipConstraint(string ownership) : StrategyNode;

    /// <summary>
    ///     类型约束
    /// </summary>
    /// <param name="type_name">类型名称。</param>
    public sealed record TypeConstraint(string type_name) : StrategyNode;

    /// <summary>
    ///     原子约束
    /// </summary>
    public sealed record AtomicConstraint : StrategyNode;

    #endregion

    #region 效应系统

    /// <summary>
    ///     效应约束，声明代码块可产生的代数效应（如 IO/State/Exception）
    /// </summary>
    /// <param name="effect">效应名称。</param>
    public sealed record EffectConstraint(string effect) : StrategyNode;

    /// <summary>
    ///     效应行，表示一组效应的并集（如 Pure | IO | State）
    /// </summary>
    /// <param name="effects">效应集合。</param>
    public sealed record EffectRow(ImmutableArray<Id> effects) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return effects;
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new EffectRow([.. effects.Select(f)]);
        }
    }

    /// <summary>
    ///     效应执行操作，在效应上下文中触发一个效应（如 perform Log("hello")）
    /// </summary>
    /// <param name="effect_name">效应名称。</param>
    /// <param name="payload">效应携带的数据。</param>
    public sealed record EffectPerform(string effect_name, Id payload) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [payload];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new EffectPerform(effect_name, f(payload));
        }
    }

    /// <summary>
    ///     效应处理器，捕获并处理效应操作（如 handle { perform Log(msg) => resume(unit) }）
    /// </summary>
    /// <param name="body">被处理的代码体。</param>
    /// <param name="effect_arms">效应处理分支列表。</param>
    public sealed record EffectHandle(Id body, ImmutableArray<Id> effect_arms) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            var ids = new List<Id>(effect_arms.Length + 1) { body };
            ids.AddRange(effect_arms);
            return ids;
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new EffectHandle(f(body), [.. effect_arms.Select(f)]);
        }
    }

    /// <summary>
    ///     效应处理分支，定义对特定效应的处理逻辑（如 perform Log(msg) => { println(msg); resume(unit) }）
    /// </summary>
    /// <param name="effect_name">要处理的效应名称。</param>
    /// <param name="payload_variable">效应数据绑定变量名。</param>
    /// <param name="resume_variable">恢复续延绑定的变量名（可选）。</param>
    /// <param name="handler_body">处理逻辑体。</param>
    public sealed record EffectArm(
        string effect_name,
        string payload_variable,
        string? resume_variable,
        Id handler_body)
        : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [handler_body];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new EffectArm(effect_name, payload_variable, resume_variable, f(handler_body));
        }
    }

    #endregion

    #region 策略包装器

    /// <summary>
    ///     执行调度策略，将计算调度到指定目标执行
    /// </summary>
    /// <param name="target">执行目标。</param>
    /// <param name="computation">被调度的计算。</param>
    public sealed record Schedule(ExecutionTarget target, Id computation) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [computation];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Schedule(target, f(computation));
        }
    }

    /// <summary>
    ///     分块策略，将计算按指定因子分块执行
    /// </summary>
    /// <param name="factor">分块因子（每块大小）。</param>
    /// <param name="computation">被分块的计算。</param>
    public sealed record Tile(int factor, Id computation) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [computation];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Tile(factor, f(computation));
        }
    }

    /// <summary>
    ///     向量化策略，将计算按指定宽度向量化
    /// </summary>
    /// <param name="width">向量化宽度（SIMD 通道数）。</param>
    /// <param name="computation">被向量化的计算。</param>
    public sealed record Vectorize(int width, Id computation) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [computation];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Vectorize(width, f(computation));
        }
    }

    /// <summary>
    ///     循环展开策略，将计算按指定因子展开
    /// </summary>
    /// <param name="factor">展开因子。</param>
    /// <param name="computation">被展开的计算。</param>
    public sealed record Unroll(int factor, Id computation) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [computation];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Unroll(factor, f(computation));
        }
    }

    /// <summary>
    ///     内存布局策略，指定数据的物理内存布局方式
    /// </summary>
    /// <param name="kind">布局类型。</param>
    /// <param name="data">目标数据。</param>
    public sealed record Layout(LayoutKind kind, Id data) : StrategyNode
    {
        /// <inheritdoc />
        public override IReadOnlyList<Id> child_ids()
        {
            return [data];
        }

        /// <inheritdoc />
        public override StrategyNode map_children(Func<Id, Id> f)
        {
            return new Layout(kind, f(data));
        }
    }

    #endregion

    #region Oa 接口实现覆盖

    /// <inheritdoc />
    public override IReadOnlyList<Id> child_ids()
    {
        return [];
    }

    /// <inheritdoc />
    public override StrategyNode map_children(Func<Id, Id> f)
    {
        return this;
    }

    #endregion
}