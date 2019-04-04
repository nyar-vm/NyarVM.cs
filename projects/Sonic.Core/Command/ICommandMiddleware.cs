using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Terminal;

namespace Core.Command;

public interface ICommandMiddleware
{
    Task<ExitCode> invoke(ICommandContext context, Func<Task<ExitCode>> next, CancellationToken cancellation_token);
}