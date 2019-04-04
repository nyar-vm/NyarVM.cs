namespace Nyar.Assembler;

/// <summary>
///     历史兼容类型：编译单元。
///     现已统一由 <see cref="GenerateModule" /> 承载。
/// </summary>
public class CompilationUnit : GenerateModule
{
    public CompilationUnit(string name) : base(name)
    {
    }
}