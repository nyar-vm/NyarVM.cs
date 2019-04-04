using System.Collections.Immutable;
using Nyar.EGraph;
using Nyar.IR.Intent;

namespace Nyar.ObjectAlgebra;

/// <summary>
///     开放节点载体，用于 EGraph 存储、规则匹配和持久化。
///     节点由 OperatorKey + 子节点 Id 列表 + 可选 Payload 组成，
///     不对节点类型做封闭枚举。
/// </summary>
public record ENode : ILanguage<ENode>
{
    /// <summary>
    ///     初始化 ENode 实例
    /// </summary>
    /// <param name="operator">操作符描述符。</param>
    /// <param name="children">子节点 Id 列表。</param>
    /// <param name="payload">可选 Payload 数据。</param>
    public ENode(IOperatorDescriptor @operator, ImmutableArray<Id> children, object? payload = null)
    {
        this.@operator = @operator;
        this.children = children;
        this.payload = payload;
    }

    /// <summary>
    ///     操作符描述符
    /// </summary>
    public IOperatorDescriptor @operator { get; }

    /// <summary>
    ///     子节点 Id 列表
    /// </summary>
    public ImmutableArray<Id> children { get; }

    /// <summary>
    ///     可选 Payload 数据
    /// </summary>
    public object? payload { get; }

    /// <summary>
    ///     使用默认相等比较器判断两个 ENode 是否结构相等
    /// </summary>
    public virtual bool Equals(ENode? other)
    {
        if (other is null) return false;

        if (!@operator.key.Equals(other.@operator.key)) return false;

        if (children.Length != other.children.Length) return false;

        for (var i = 0; i < children.Length; i++)
            if (!children[i].Equals(other.children[i]))
                return false;

        if (payload is not null) return payload.Equals(other.payload);

        return other.payload is null;
    }

    /// <inheritdoc />
    public IReadOnlyList<Id> child_ids()
    {
        return children;
    }

    /// <inheritdoc />
    public ENode map_children(Func<Id, Id> f)
    {
        if (children.Length == 0) return this;

        var mapped = children.Select(f).ToImmutableArray();
        return new ENode(@operator, mapped, payload);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(@operator.key);
        foreach (var child in children) hash.Add(child);

        if (payload is not null) hash.Add(payload);

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return payload is not null
            ? $"{@operator.name}({string.Join(", ", children)}, payload={payload})"
            : $"{@operator.name}({string.Join(", ", children)})";
    }
}