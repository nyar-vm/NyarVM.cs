namespace Std.Data.Text.Erlang;

public sealed class ErlangLanguage : Language
{
    public override string Name => "Erlang";
    public bool MapsEnabled { get; init; } = true;
    public bool BinaryEnabled { get; init; } = true;
    public bool OtpBehaviorsEnabled { get; init; } = true;

    public static ErlangLanguage Default => new();
    public static ErlangLanguage OTP26 => new()
    {
        MapsEnabled = true,
        BinaryEnabled = true,
        OtpBehaviorsEnabled = true
    };
}
