using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.Lexer;

public static class ValkyrieNodeKindExtensions
{
    public static NodeKind to_node_kind(this ValkyrieTokenKind kind)
    {
        return new NodeKind((int)kind);
    }

    /// <param name="kind">。</param>
    extension(NodeKind kind)
    {
        /// <summary>
        ///     约定 100 到 1000 为关键词区
        /// </summary>
        /// <returns>。</returns>
        public bool is_keyword()
        {
            var vKind = (ValkyrieTokenKind)kind.value;
            return vKind is >= ValkyrieTokenKind.let and <= ValkyrieTokenKind.@as
                or ValkyrieTokenKind.@class
                or ValkyrieTokenKind.structure
                or ValkyrieTokenKind.trait
                or ValkyrieTokenKind.imply
                or ValkyrieTokenKind.@namespace
                or ValkyrieTokenKind.@using
                or ValkyrieTokenKind.type
                or ValkyrieTokenKind.raise
                or ValkyrieTokenKind.yield
                or ValkyrieTokenKind.@try
                or ValkyrieTokenKind.component
                or ValkyrieTokenKind.system
                or ValkyrieTokenKind.widget
                or ValkyrieTokenKind.model
                or ValkyrieTokenKind.service
                or ValkyrieTokenKind.shader
                or ValkyrieTokenKind.neural
                or ValkyrieTokenKind.uniform
                or ValkyrieTokenKind.varying
                or ValkyrieTokenKind.c_buffer
                or ValkyrieTokenKind.texture
                or ValkyrieTokenKind.sampler
                or ValkyrieTokenKind.discard;
        }


        /// <summary>
        ///     判断是否为语句关键字而非声明关键字。
        ///     语句关键字包括：case、else、return、while、if、match、break、continue、
        ///     raise、yield、try、catch、resume、when、in、is、as、until、end、loop。
        /// </summary>
        /// <returns>如果是语句关键字返回 true，否则返回 false。</returns>
        public bool is_statement_keyword()
        {
            var vKind = (ValkyrieTokenKind)kind.value;
            return vKind is ValkyrieTokenKind.@case
                or ValkyrieTokenKind.@else
                or ValkyrieTokenKind.@return
                or ValkyrieTokenKind.@while
                or ValkyrieTokenKind.@if
                or ValkyrieTokenKind.match
                or ValkyrieTokenKind.@break
                or ValkyrieTokenKind.@continue
                or ValkyrieTokenKind.raise
                or ValkyrieTokenKind.yield
                or ValkyrieTokenKind.@try
                or ValkyrieTokenKind.@catch
                or ValkyrieTokenKind.resume
                or ValkyrieTokenKind.when
                or ValkyrieTokenKind.@in
                or ValkyrieTokenKind.@is
                or ValkyrieTokenKind.@as
                or ValkyrieTokenKind.until
                or ValkyrieTokenKind.end
                or ValkyrieTokenKind.loop;
        }

        /// <summary>
        ///     约定 2000 以上为操作符区
        /// </summary>
        /// <returns>。</returns>
        public bool is_operator()
        {
            var vKind = (ValkyrieTokenKind)kind.value;
            return vKind is >= ValkyrieTokenKind.plus and <= ValkyrieTokenKind.dot_dot_dot
                or ValkyrieTokenKind.less
                or ValkyrieTokenKind.greater;
        }

        public bool is_literal()
        {
            var vKind = (ValkyrieTokenKind)kind.value;
            return vKind is >= ValkyrieTokenKind.@true and <= ValkyrieTokenKind.@null;
        }
    }
}