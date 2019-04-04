using Std.Data.Text.Notedown.Syntax;

namespace Std.Data.Text.Syntax;

/// <summary>
///     Oak 语言描述基类，承载名称、继承关系以及与 Notedown IR 的双向转换能力
///     每种格式的 Language 实现是唯一入口，负责：
///     1. 解析自身格式文本 → 自身 AST
///     2. 自身 AST → NotedownDocument（IR）
///     3. NotedownDocument（IR） → 自身 AST
///     4. 自身 AST → 自身格式文本
/// </summary>
public abstract class Language
{
    /// <summary>
    ///     语言名称
    /// </summary>
    public abstract string name { get; }

    /// <summary>
    ///     基本语言（用于继承配置）
    /// </summary>
    public virtual Language? @base => null;

    /// <summary>
    ///     将本语言原生 AST 转换为 NotedownDocument（IR）
    ///     默认抛出 NotSupportedException，子类按需覆写
    /// </summary>
    /// <param name="ast">本语言原生 AST（具体类型由各 Language 实现决定）</param>
    /// <returns>Notedown 统一文档 IR</returns>
    public virtual NotedownDocument to_notedown(object ast)
    {
        throw new NotSupportedException($"{name} 不支持转换为 Notedown IR");
    }

    /// <summary>
    ///     将 NotedownDocument（IR）转换为本语言原生 AST 或格式化文本
    ///     默认抛出 NotSupportedException，子类按需覆写
    /// </summary>
    /// <param name="document">Notedown 统一文档 IR</param>
    /// <returns>本语言原生 AST 或格式化文本字符串</returns>
    public virtual object from_notedown(NotedownDocument document)
    {
        throw new NotSupportedException($"{name} 不支持从 Notedown IR 转换");
    }

    public override string ToString()
    {
        return name;
    }
}