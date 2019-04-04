using System.Diagnostics;
using System.Text;
using Nyar.PackageManager.Package;
using Nyar.PackageManager.Workspace;

namespace Nyar.PackageManager.Scripts;

public class ScriptRunner
{
    private readonly Dictionary<string, string> _environment_variables;
    private readonly string _working_directory;

    public ScriptRunner(string workingDirectory, Dictionary<string, string>? environmentVariables = null)
    {
        _working_directory = workingDirectory;
        _environment_variables = environmentVariables ?? new Dictionary<string, string>();
    }

    public async Task<ScriptResult> run(string script)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var (command, arguments) = parse_command(script);

            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = _working_directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var env in _environment_variables) processInfo.Environment[env.Key] = env.Value;

            using var process = new Process();
            process.StartInfo = processInfo;

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data is not null) outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data is not null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Run(() => process.WaitForExit());

            stopwatch.Stop();

            return new ScriptResult
            {
                success = process.ExitCode == 0,
                exit_code = process.ExitCode,
                output = outputBuilder.ToString(),
                error = errorBuilder.ToString(),
                duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new ScriptResult
            {
                success = false,
                exit_code = -1,
                output = string.Empty,
                error = ex.Message,
                duration = stopwatch.Elapsed
            };
        }
    }

    public async Task<ScriptResult> run_script(LegionManifest manifest, string scriptName)
    {
        if (!manifest.has_script(scriptName))
            return new ScriptResult
            {
                success = false,
                exit_code = -1,
                error = $"脚本 '{scriptName}' 在 legion.von 中未定义"
            };

        var script = manifest.get_script(scriptName);
        Console.WriteLine($"> legion run {scriptName}");
        Console.WriteLine($"> {script}");

        return await run(script);
    }

    public async Task<ScriptResult> run_script(LegionsWorkspace workspace, string scriptName)
    {
        if (!workspace.has_script(scriptName))
            return new ScriptResult
            {
                success = false,
                exit_code = -1,
                error = $"脚本 '{scriptName}' 在 voa.workspace.v 中未定义"
            };

        var script = workspace.get_script(scriptName);
        Console.WriteLine($"> legion run {scriptName}");
        Console.WriteLine($"> {script}");

        return await run(script);
    }

    public async Task<ScriptResult> run_lifecycle_hook(string hookName, LegionManifest manifest)
    {
        if (!manifest.has_script(hookName))
            return new ScriptResult
            {
                success = true,
                exit_code = 0,
                output = $"生命周期钩子 '{hookName}' 未定义，跳过"
            };

        Console.WriteLine($"> 执行生命周期钩子: {hookName}");
        return await run_script(manifest, hookName);
    }

    public List<string> list_scripts(LegionManifest manifest)
    {
        return [.. manifest.scripts.Keys];
    }

    public List<string> list_scripts(LegionsWorkspace workspace)
    {
        return [.. workspace.scripts.Keys];
    }

    private (string command, string arguments) parse_command(string script, string? shell = null)
    {
        script = script.Trim();

        if (script.StartsWith("legion "))
        {
            var legionCommand = script[7..].Trim();
            return ("legion", legionCommand);
        }

        if (script.StartsWith("vcc "))
        {
            var vccCommand = script[4..].Trim();
            return ("vcc", vccCommand);
        }

        if (!string.IsNullOrEmpty(shell))
            return shell.ToLowerInvariant() switch
            {
                "powershell" or "pwsh" => ("pwsh", $"-Command \"{script.Replace("\"", "\\\"")}\""),
                "bash" => ("/bin/bash", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "sh" => ("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "zsh" => ("/bin/zsh", $"-c \"{script.Replace("\"", "\\\"")}\""),
                "cmd" => ("cmd", $"/c {script}"),
                _ => ("cmd", $"/c {script}")
            };

        if (OperatingSystem.IsWindows()) return ("cmd", $"/c {script}");

        return ("/bin/sh", $"-c \"{script.Replace("\"", "\\\"")}\"");
    }
}