namespace Std.Data.Text.C;

public sealed class CLanguage : ILanguage
{
    
    public ILanguage? Base => null;
    public bool C11Enabled { get; init; } = true;
    public bool C99Enabled { get; init; } = true;

    public static CLanguage Default => new();
}