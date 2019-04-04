using System.Diagnostics;
using Core.Process;

namespace Std.Process;

/// <summary>
///     进程构建器，实现 <see cref="IProcessBuilder" /> 接口，
///     提供流式配置进程参数的能力。
/// </summary>
public sealed class ProcessBuilder : IProcessBuilder
{
    /// <summary>
    ///     命令行参数列表。
    /// </summary>
    private readonly List<string> _arguments = [];

    /// <summary>
    ///     环境变量字典。
    /// </summary>
    private readonly Dictionary<string, string> _environment_variables = new();

    /// <summary>
    ///     可执行文件路径。
    /// </summary>
    private readonly string _file_name;

    /// <summary>
    ///     工作目录路径。
    /// </summary>
    private string? _working_directory;

    /// <summary>
    ///     初始化 <see cref="ProcessBuilder" /> 的新实例。
    /// </summary>
    /// <param name="fileName">可执行文件路径。</param>
    public ProcessBuilder(string fileName)
    {
        _file_name = fileName;
    }

    /// <summary>
    ///     添加命令行参数。
    /// </summary>
    /// <param name="argument">命令行参数。</param>
    /// <returns>当前构建器实例。</returns>
    public IProcessBuilder with_argument(string argument)
    {
        _arguments.Add(argument);
        return this;
    }

    /// <summary>
    ///     设置工作目录。
    /// </summary>
    /// <param name="path">工作目录路径。</param>
    /// <returns>当前构建器实例。</returns>
    public IProcessBuilder with_working_directory(string path)
    {
        _working_directory = path;
        return this;
    }

    /// <summary>
    ///     设置环境变量。
    /// </summary>
    /// <param name="name">环境变量名称。</param>
    /// <param name="value">环境变量值。</param>
    /// <returns>当前构建器实例。</returns>
    public IProcessBuilder with_environment_variable(string name, string value)
    {
        _environment_variables[name] = value;
        return this;
    }

    /// <summary>
    ///     启动进程。
    /// </summary>
    /// <returns>启动后的进程实例。</returns>
    public IProcess start()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _file_name
        };

        foreach (var arg in _arguments) startInfo.ArgumentList.Add(arg);

        if (_working_directory is not null) startInfo.WorkingDirectory = _working_directory;

        foreach (var (name, value) in _environment_variables) startInfo.Environment[name] = value;

        var process = new System.Diagnostics.Process
        {
            StartInfo = startInfo
        };

        process.Start();

        return new ProcessRunner(process);
    }
}