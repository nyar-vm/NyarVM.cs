namespace Commander.Testing.Commands;

public partial class BuildCommand
{
    public static BuildCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static bool TryParse(string[] args, out BuildCommand? command, out string? error)
    {
        command = null;
        error = "源生成器未适配 Sonic.Standard.Command 命名空间";
        return false;
    }

    public static string GetHelpText()
    {
        throw new NotSupportedException();
    }

    public static string GetJsonSchema()
    {
        throw new NotSupportedException();
    }

    public static string GetCompletionScript(Sonic.Command.ShellType shell)
    {
        throw new NotSupportedException();
    }
}

public partial class CalcCommand
{
    public static CalcCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static string GetHelpText()
    {
        throw new NotSupportedException();
    }
}

public partial class DeployCommand
{
    public static DeployCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static bool TryParse(string[] args, out DeployCommand? command, out string? error)
    {
        command = null;
        error = "源生成器未适配 Sonic.Standard.Command 命名空间";
        return false;
    }

    public static string GetHelpText()
    {
        throw new NotSupportedException();
    }
}

public partial class EchoCommand
{
    public static EchoCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static bool TryParse(string[] args, out EchoCommand? command, out string? error)
    {
        command = null;
        error = "源生成器未适配 Sonic.Standard.Command 命名空间";
        return false;
    }
}

public partial class GreetCommand
{
    public static GreetCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static bool TryParse(string[] args, out GreetCommand? command, out string? error)
    {
        command = null;
        error = "源生成器未适配 Sonic.Standard.Command 命名空间";
        return false;
    }

    public static string GetHelpText()
    {
        throw new NotSupportedException();
    }

    public static string GetCompletionScript(Sonic.Command.ShellType shell)
    {
        throw new NotSupportedException();
    }

    public static string GetJsonSchema()
    {
        throw new NotSupportedException();
    }
}

public partial class MigrateCommand
{
    public static MigrateCommand Parse(string[] args)
    {
        throw new NotSupportedException("源生成器未适配 Sonic.Standard.Command 命名空间");
    }

    public static string GetHelpText()
    {
        throw new NotSupportedException();
    }
}