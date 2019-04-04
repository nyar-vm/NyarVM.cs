using Nyar.Assembler;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Types.Targets;

namespace Nyar.Language.Valkyrie.Compiler.Packaging;

/// <summary>
///     target packaging 抽象。
/// </summary>
public interface ITargetPackager
{
    ArtifactSet package(
        string moduleName,
        OutputSpec generated,
        TargetProfile targetProfile);
}