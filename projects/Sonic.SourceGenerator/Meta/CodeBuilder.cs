using System.Text;

namespace Sonic.Data.Generator.Meta;

/// <summary>
///     流式代码生成辅助器，提供缩进感知的流畅 C# 代码构建能力。
///     不基于字符串拼接，采用 <see cref="StringBuilder" /> 内部缓冲。
/// </summary>
public class CodeBuilder
{
    private readonly StringBuilder _builder;
    private int _indent;

    /// <summary>
    ///     初始化代码构建器的新实例。
    /// </summary>
    public CodeBuilder()
    {
        _builder = new StringBuilder();
        _indent = 0;
    }

    /// <summary>
    ///     增加一级缩进。
    /// </summary>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder indent()
    {
        _indent++;
        return this;
    }

    /// <summary>
    ///     减少一级缩进。
    /// </summary>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder outdent()
    {
        if (_indent > 0) _indent--;

        return this;
    }

    /// <summary>
    ///     在当前位置追加一行带缩进的文本。
    /// </summary>
    /// <param name="text">要追加的文本内容。若为 <c>null</c> 则追加空行。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder append_line(string? text = null)
    {
        if (text is not null)
        {
            _builder.Append(new string(' ', _indent * 4));
            _builder.AppendLine(text);
        }
        else
        {
            _builder.AppendLine();
        }

        return this;
    }

    /// <summary>
    ///     追加一个以花括号包裹的代码块，并在块内自动增加缩进。
    /// </summary>
    /// <param name="header">代码块头部声明（不含左花括号），如 <c>"public class MyClass"</c>。</param>
    /// <param name="body">在代码块内部执行的构建操作。</param>
    /// <param name="semicolon">是否在右花括号后追加分号。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder append_block(string header, Action<CodeBuilder> body, bool semicolon = false)
    {
        append_line(header);
        append_line("{");
        indent();
        body(this);
        outdent();

        if (semicolon)
            append_line("};");
        else
            append_line("}");

        return this;
    }

    /// <summary>
    ///     追加一个命名空间声明块。
    /// </summary>
    /// <param name="name">命名空间名称。</param>
    /// <param name="body">命名空间内部的构建操作。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder @namespace(string name, Action<CodeBuilder> body)
    {
        return append_block($"namespace {name}", body);
    }

    /// <summary>
    ///     追加一个类声明块。
    /// </summary>
    /// <param name="modifiers">修饰符，如 <c>"public static partial"</c>。</param>
    /// <param name="name">类名。</param>
    /// <param name="body">类体内部的构建操作。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder @class(string modifiers, string name, Action<CodeBuilder> body)
    {
        return append_block($"{modifiers} class {name}", body);
    }

    /// <summary>
    ///     追加一个结构体声明块。
    /// </summary>
    /// <param name="modifiers">修饰符，如 <c>"public readonly partial"</c>。</param>
    /// <param name="name">结构体名。</param>
    /// <param name="body">结构体内部的构建操作。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder @struct(string modifiers, string name, Action<CodeBuilder> body)
    {
        return append_block($"{modifiers} struct {name}", body);
    }

    /// <summary>
    ///     追加一个接口声明块。
    /// </summary>
    /// <param name="modifiers">修饰符。</param>
    /// <param name="name">接口名。</param>
    /// <param name="body">接口内部的构建操作。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder @interface(string modifiers, string name, Action<CodeBuilder> body)
    {
        return append_block($"{modifiers} interface {name}", body);
    }

    /// <summary>
    ///     追加一个方法声明块。
    /// </summary>
    /// <param name="modifiers">修饰符，如 <c>"public static"</c>。</param>
    /// <param name="returnType">返回类型。</param>
    /// <param name="name">方法名。</param>
    /// <param name="parameters">参数字符串，如 <c>"int x, string y"</c>。</param>
    /// <param name="body">方法体内部的构建操作。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder method(string modifiers, string returnType, string name, string parameters,
        Action<CodeBuilder> body)
    {
        return append_block($"{modifiers} {returnType} {name}({parameters})", body);
    }

    /// <summary>
    ///     追加一个属性声明，带 getter 和可选的 setter。
    /// </summary>
    /// <param name="modifiers">修饰符。</param>
    /// <param name="type">属性类型。</param>
    /// <param name="name">属性名。</param>
    /// <param name="getterBody">getter 内部构建操作（至少需要输出 <c>return</c> 或 <c>=&gt;</c> 表达式）。</param>
    /// <param name="setterBody">setter 内部构建操作，为 <c>null</c> 表示只读属性。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder property(string modifiers, string type, string name, Action<CodeBuilder>? getterBody = null,
        Action<CodeBuilder>? setterBody = null)
    {
        append_line($"{modifiers} {type} {name}");
        append_line("{");
        indent();

        if (getterBody is not null)
        {
            append_line("get");
            append_line("{");
            indent();
            getterBody(this);
            outdent();
            append_line("}");
        }

        if (setterBody is not null)
        {
            append_line("set");
            append_line("{");
            indent();
            setterBody(this);
            outdent();
            append_line("}");
        }

        outdent();
        append_line("}");
        return this;
    }

    /// <summary>
    ///     追加一个简洁的单行表达式体属性。
    /// </summary>
    /// <param name="modifiers">修饰符。</param>
    /// <param name="type">属性类型。</param>
    /// <param name="name">属性名。</param>
    /// <param name="expression">表达式体内容，如 <c>"_value"</c>。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder auto_property(string modifiers, string type, string name, string expression)
    {
        return append_line($"{modifiers} {type} {name} => {expression};");
    }

    /// <summary>
    ///     追加一个字段声明。
    /// </summary>
    /// <param name="modifiers">修饰符。</param>
    /// <param name="type">字段类型。</param>
    /// <param name="name">字段名。</param>
    /// <param name="initializer">初始化表达式，为 <c>null</c> 表示无初始化。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder field(string modifiers, string type, string name, string? initializer = null)
    {
        if (initializer is not null) return append_line($"{modifiers} {type} {name} = {initializer};");

        return append_line($"{modifiers} {type} {name};");
    }

    /// <summary>
    ///     追加一个 using 指令。
    /// </summary>
    /// <param name="namespaceName">要引用的命名空间。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder @using(string namespaceName)
    {
        return append_line($"using {namespaceName};");
    }

    /// <summary>
    ///     追加 XML 文档注释。
    /// </summary>
    /// <param name="summary">摘要内容。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder xml_summary(string summary)
    {
        append_line("/// <summary>");
        append_line($"/// {summary}");
        append_line("/// </summary>");
        return this;
    }

    /// <summary>
    ///     返回已构建的代码字符串。
    /// </summary>
    /// <returns>生成的完整代码文本。</returns>
    public override string ToString()
    {
        return _builder.ToString();
    }

    /// <summary>
    ///     清空所有已构建内容，重置缩进。
    /// </summary>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public CodeBuilder clear()
    {
        _builder.Clear();
        _indent = 0;
        return this;
    }
}