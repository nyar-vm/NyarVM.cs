using System.Threading;
using System.Threading.Tasks;
using Core.Terminal;

namespace Core.Command;

public interface ICommand
{
    Task<ExitCode> execute(ICommandContext context, CancellationToken cancel);
}