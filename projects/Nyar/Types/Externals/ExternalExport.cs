namespace Nyar.Types.Externals;

/// <summary>
///     HIR 阶段的外部导入链接语义。
/// </summary>
public abstract record ExternalExport
{
    /// <summary>
    ///     HIR 阶段的外部导入链接语义。
    /// </summary>
    protected ExternalExport(CallingConvention convention)
    {
        this.convention = convention;
    }

    public CallingConvention convention { get; init; }
}