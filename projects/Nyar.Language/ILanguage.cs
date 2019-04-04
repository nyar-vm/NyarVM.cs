namespace Nyar.Language;

/// <summary>
///     语言标识接口，仅作为标识符和继承关系的载体
/// </summary>
public interface ILanguage
{
    string name { get; }
    ILanguage? @base { get; }
}