using Nyar.Assembler;

namespace Nyar.Language.Valkyrie.Compiler.Lir;

/// <summary>
///     低层中间表示模块�?///     当前�?`GenerateModule` 作为 `Nyar Standard IR` 的承载结构�?///
/// </summary>
public sealed class LirModule
{
    public LirModule(GenerateModule module)
    {
        this.module = module;
    }

    public GenerateModule module { get; }

    public string name => module.name;
}