using Core.Data;
using Std.Config;

namespace Nyar.Language.Valkyrie.Config;

[Data]
public sealed class VoaHotReloadConfig
{
    public bool enabled { get; set; } = true;
    [Merge(MergeMode.union)]
    public List<string> watch { get; set; } = ["source/", "assets/"];
    [Merge(MergeMode.union)]
    public List<string> ignore { get; set; } = [".git/", "node_modules/"];
    public int debounce { get; set; } = 100;
}
