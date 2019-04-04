using Core.Terminal;
using Std.Command.Builder;
using Std.Command.TypeConversion;
using ICommand = Core.Command.ICommand;

namespace Std.Command;

/// <summary>
///     命令行解析器，提供参数解析和命令执行功能
/// </summary>
public static class CommandParser
{
    /// <summary>
    ///     类型转换注册表
    /// </summary>
    public static TypeConverterRegistry registry { get; } = new();

    /// <summary>
    ///     将字符串值转换为目标类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="value">字符串值</param>
    /// <returns>转换后的值</returns>
    public static T convert_value<T>(string value)
    {
        if (registry.try_convert<T>(value, out var result)) return result;

        return (T)Convert.ChangeType(value, typeof(T));
    }

    /// <summary>
    ///     尝试解析命令行参数为命令实例
    /// </summary>
    /// <typeparam name="T">命令类型</typeparam>
    /// <param name="args">命令行参数</param>
    /// <param name="command">解析后的命令实例</param>
    /// <param name="errors">解析错误列表</param>
    /// <returns>是否解析成功</returns>
    public static bool try_parse<T>(string[] args, out T? command, out List<string> errors) where T : ICommand, new()
    {
        command = default;
        errors = [];

        try
        {
            command = new T();
            return true;
        }
        catch (Exception ex)
        {
            errors.Add(ex.Message);
            return false;
        }
    }

    /// <summary>
    ///     解析并执行命令模型
    /// </summary>
    /// <param name="model">命令模型</param>
    /// <param name="args">命令行参数</param>
    /// <returns>退出码</returns>
    public static ExitCode parse_and_execute(BuiltCommandModel model, string[] args)
    {
        if (model.handler != null)
        {
            var result = model.handler.DynamicInvoke();
            if (result is ExitCode exitCode) return exitCode;

            if (result is int intCode) return (ExitCode)intCode;
        }

        return ExitCode.Success;
    }
}