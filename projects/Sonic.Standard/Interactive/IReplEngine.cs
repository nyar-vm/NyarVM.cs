namespace Sonic.Interactive;

public interface IReplEngine
{
    Task run(CancellationToken cancellationToken = default);
}
