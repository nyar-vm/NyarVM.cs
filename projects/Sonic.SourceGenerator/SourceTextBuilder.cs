using System.Text;

namespace Sonic.Data.Generator;

/// <summary>
///     支持缩进块管理的源代码文本构建器，供 Source Generator 内部使用。
///     提供 <c>Block()</c> 方法自动管理大括号和缩进。
/// </summary>
internal sealed class SourceTextBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indent;

    /// <summary>
    ///     在当前缩进级别追加一行文本。
    /// </summary>
    /// <param name="text">要追加的文本行（可含前导空白自动对齐）。</param>
    public void append_line(string text)
    {
        if (text.Length > 0) _sb.Append(' ', _indent * 4);

        _sb.AppendLine(text);
    }

    /// <summary>
    ///     在当前缩进级别追加文本（无换行）。
    /// </summary>
    /// <param name="text">要追加的文本。</param>
    public void append(string text)
    {
        _sb.Append(text);
    }

    /// <summary>
    ///     追加一个空行。
    /// </summary>
    public void append_line()
    {
        _sb.AppendLine();
    }

    /// <summary>
    ///     开始一个大括号块并自动增加缩进，返回一个在 <c>using</c> 块结束时自动关闭大括号的释放器。
    /// </summary>
    /// <param name="suffix">右大括号后的后缀（如 <c>";"</c>），默认为空。</param>
    public BlockDisposer block(string suffix = "")
    {
        _sb.Append(' ', _indent * 4);
        _sb.AppendLine("{");
        _indent++;
        return new BlockDisposer(this, suffix);
    }

    /// <summary>
    ///     增加一级缩进。
    /// </summary>
    public SourceTextBuilder indent()
    {
        _indent++;
        return this;
    }

    /// <summary>
    ///     减少一级缩进。
    /// </summary>
    public SourceTextBuilder outdent()
    {
        if (_indent > 0) _indent--;

        return this;
    }

    /// <summary>
    ///     返回构建的完整源代码文本。
    /// </summary>
    /// <returns>生成的全部源代码字符串。</returns>
    public override string ToString()
    {
        return _sb.ToString();
    }

    /// <summary>
    ///     大括号块释放器，在 <c>using</c> 块释放时自动闭合并缩进。
    /// </summary>
    public readonly struct BlockDisposer : IDisposable
    {
        private readonly SourceTextBuilder _builder;
        private readonly string _suffix;

        internal BlockDisposer(SourceTextBuilder builder, string suffix)
        {
            _builder = builder;
            _suffix = suffix;
        }

        /// <summary>
        ///     释放时输出右大括号并恢复缩进。
        /// </summary>
        public void Dispose()
        {
            _builder._indent--;
            _builder._sb.Append(' ', _builder._indent * 4);
            _builder._sb.Append('}');

            if (!string.IsNullOrEmpty(_suffix)) _builder._sb.Append(_suffix);

            _builder._sb.AppendLine();
        }
    }
}