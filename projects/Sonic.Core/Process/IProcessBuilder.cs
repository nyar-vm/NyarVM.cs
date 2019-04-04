namespace Core.Process;

/// <summary>
///     进程构建器接口，提供流式配置进程参数的能力
/// </summary>
public interface IProcessBuilder
{
    /// <summary>
    ///     添加命令行参数
    /// </summary>
    /// <param name="argument">命令行参数</param>
    IProcessBuilder with_argument(string argument);

    /// <summary>
    ///     设置工作目录
    /// </summary>
    /// <param name="path">工作目录路径</param>
    IProcessBuilder with_working_directory(string path);

    /// <summary>
    ///     设置环境变量
    /// </summary>
    /// <param name="name">环境变量名称</param>
    /// <param name="value">环境变量值</param>
    IProcessBuilder with_environment_variable(string name, string value);

    /// <summary>
    ///     启动进程
    /// </summary>
    IProcess start();
}