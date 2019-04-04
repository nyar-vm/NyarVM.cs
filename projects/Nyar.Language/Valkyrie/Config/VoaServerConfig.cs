using Core.Data;

namespace Nyar.Language.Valkyrie.Config;

[Data]
public sealed class VoaServerConfig
{
    public string host { get; set; } = "localhost";
    public int port { get; set; } = 3000;
    public int workers { get; set; } = 0;
    public int timeout { get; set; } = 30;
}
