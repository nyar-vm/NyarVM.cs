namespace Std.Data.Text.Valkyrie.AST.Type;

public enum TypeUnaryOperator
{
    /// <summary>
    ///     !T
    /// </summary>
    not,

    /// <summary>
    ///     -T
    /// </summary>
    contravariance,

    /// <summary>
    ///     +T
    /// </summary>
    covariance,

    /// <summary>
    ///     T?
    /// </summary>
    nullable
}