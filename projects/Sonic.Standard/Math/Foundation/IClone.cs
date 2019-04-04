namespace Std.Math.Foundation;

public interface IClone<TSelf>
    where TSelf : IClone<TSelf>
{
    TSelf clone();
}