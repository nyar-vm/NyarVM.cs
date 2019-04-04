namespace Nyar.Language;

/// <summary>
///     语言描述基类，仅承载名称与继承关系，不感知任何具体 Provider 类型。
/// </summary>
public abstract class Language : ILanguage
{
    public abstract string name { get; }

    public virtual ILanguage? @base => null;

    public override string ToString()
    {
        return name;
    }
}