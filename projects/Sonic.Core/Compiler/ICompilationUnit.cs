namespace Core.Compiler;

/// <summary>
///     编译单元接口，表示编译过程中的一个独立单元
/// </summary>
public interface ICompilationUnit
{
    /// <summary>
    ///     获取编译单元的名称
    /// </summary>
    string name { get; }
}