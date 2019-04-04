namespace Std.Collection;

public interface IFrontAccessible<T>
{
    T front();
    T back();
}