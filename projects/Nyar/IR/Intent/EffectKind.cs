namespace Nyar.IR.Intent;

public enum EffectKind
{
    read,
    write,
    allocate,
    free,
    io,
    @throw,
    capture_cc,
    choice_point,
    perform,
    handle
}