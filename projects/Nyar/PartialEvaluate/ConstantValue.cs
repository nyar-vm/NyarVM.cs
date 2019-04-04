using Nyar.Dialect.Core.Nodes;
using Nyar.IR.Intent;

namespace Nyar.PartialEvaluate;

/// <summary>
///     常量值表示
/// </summary>
public abstract record ConstantValue
{
    /// <summary>
    ///     转换为 Oa 节点
    /// </summary>
    public abstract AlgebraNode to_i_kun();

    /// <summary>
    ///     整数常量
    /// </summary>
    public sealed record Int64(long value) : ConstantValue
    {
        /// <inheritdoc />
        public override AlgebraNode to_i_kun()
        {
            return new Literal<long>(value);
        }
    }

    /// <summary>
    ///     浮点常量
    /// </summary>
    public sealed record Float64(double value) : ConstantValue
    {
        /// <inheritdoc />
        public override AlgebraNode to_i_kun()
        {
            return new Literal<double>(value);
        }
    }

    /// <summary>
    ///     布尔常量
    /// </summary>
    public sealed record Bool(bool value) : ConstantValue
    {
        /// <inheritdoc />
        public override AlgebraNode to_i_kun()
        {
            return new Literal<bool>(value);
        }
    }

    /// <summary>
    ///     字符串常量
    /// </summary>
    public sealed record String(string value) : ConstantValue
    {
        /// <inheritdoc />
        public override AlgebraNode to_i_kun()
        {
            return new Literal<string>(value);
        }
    }
}