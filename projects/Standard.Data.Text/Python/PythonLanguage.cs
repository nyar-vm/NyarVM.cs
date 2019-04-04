namespace Std.Data.Text.Python;

public sealed class PythonLanguage : Language
{
    public override string name => "Python";

    public bool Python3Enabled { get; init; } = true;
    public bool TypeHintsEnabled { get; init; } = true;
    public bool WalrusOperatorEnabled { get; init; } = true;
    public bool MatchStatementEnabled { get; init; }

    public static PythonLanguage Default => new();
    public static PythonLanguage Python310 => new() { MatchStatementEnabled = true };
}
