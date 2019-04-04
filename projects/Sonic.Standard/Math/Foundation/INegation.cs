namespace Std.Math.Foundation;

public interface INegation<TSelf, TResult>
    where TSelf : INegation<TSelf, TResult>
{
    TResult neg();
}