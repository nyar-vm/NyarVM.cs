using System;
using System.Collections.Generic;
using System.Threading;
using Core.Console;
using Core.Terminal;

namespace Core.Command;

public interface ICommandContext
{
    IReadOnlyDictionary<string, object?> arguments { get; }

    IServiceProvider services { get; }

    IConsole? console { get; }

    IOutputWriter? output { get; }

    IInputReader? input { get; }

    ITerminalRenderer? renderer { get; }

    ILocalizer? localizer { get; }

    CancellationToken cancellation_token { get; }

    T? get_option_value<T>(string name);
}