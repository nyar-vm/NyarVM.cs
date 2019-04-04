using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Core.Command;
using Core.Command.Argument;
using Core.Command.Option;
using Core.Data.Validation;
using Std.Command.Builder;
using Std.Command.Help;
using Std.Command.TypeConversion;

namespace Std.Command;

/// <summary>
///     命令行参数解析器，支持属性驱动模式、构建器模式和函数式模式
/// </summary>
public static class CliArgumentParser
{
    /// <summary>
    ///     SourceGen 调度解析，优先使用编译期生成的 __Parse 方法，否则回退到反射
    /// </summary>
    /// <typeparam name="T">命令模型类型</typeparam>
    /// <param name="args">命令行参数</param>
    /// <param name="command">解析后的命令实例，失败时为 default</param>
    /// <param name="errors">错误列表，成功时为空</param>
    /// <returns>是否解析成功</returns>
    public static bool try_parse<T>(string[] args, [MaybeNullWhen(false)] out T command, out List<string> errors)
        where T : ICommand, new()
    {
        var parseMethod = typeof(T).GetMethod("__Parse", BindingFlags.Public | BindingFlags.Static);
        if (parseMethod is not null)
        {
            command = new T();
            errors = [];
            var parameters = new object?[] { args, command, errors };
            parseMethod.Invoke(null, parameters);
            command = (T)parameters[1]!;
            errors = (List<string>)parameters[2]!;
            return errors.Count == 0;
        }

        var result = parse<T>(args);
        command = result.success ? result.model! : default;
        errors = [];
        if (!result.success) errors.Add(result.error_message);

        return result.success;
    }

    /// <summary>
    ///     解析命令行参数为强类型模型（属性驱动模式），支持子命令路由和剩余参数
    /// </summary>
    /// <typeparam name="T">命令模型类型</typeparam>
    /// <param name="args">命令行参数</param>
    public static ParseResult<T> parse<T>(string[] args) where T : new()
    {
        var result = new ParseResult<T>();
        var model = new T();
        var type = typeof(T);

        var argProperties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<ArgumentAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<ArgumentAttribute>()!.position)
            .ToList();

        var optionProperties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<OptionAttribute>() != null)
            .ToList();

        var subCommandProperties = type.GetProperties()
            .Where(p => p.GetCustomAttribute<SubcommandAttribute>() != null)
            .ToList();

        var argIndex = 0;
        var i = 0;
        var hasRemainingArg = argProperties.Count > 0 && is_remaining_args_type(argProperties[^1].PropertyType);

        while (i < args.Length)
        {
            var arg = args[i];

            if (arg is "--help" or "-h")
            {
                result.success = true;
                result.model = model;
                return result;
            }

            if (arg == "--")
            {
                i++;
                while (i < args.Length && (argIndex < argProperties.Count || hasRemainingArg))
                {
                    if (hasRemainingArg && argIndex == argProperties.Count - 1)
                    {
                        append_remaining_arg(model, argProperties[^1], args[i..]);
                        i = args.Length;
                        break;
                    }

                    if (argIndex < argProperties.Count)
                    {
                        set_property_value(model, argProperties[argIndex], args[i]);
                        argIndex++;
                    }

                    i++;
                }

                break;
            }

            if (arg.StartsWith("--"))
            {
                var eqIndex = arg.IndexOf('=');
                string optName;
                string? optValue;

                if (eqIndex >= 0)
                {
                    optName = arg[2..eqIndex];
                    optValue = arg[(eqIndex + 1)..];
                }
                else
                {
                    optName = arg[2..];
                    optValue = null;
                }

                var matched = find_option_property(optionProperties, optName);
                if (matched != null)
                {
                    var propType = matched.PropertyType;
                    if (propType == typeof(bool))
                    {
                        matched.SetValue(model, true);
                    }
                    else
                    {
                        optValue ??= i + 1 < args.Length && !args[i + 1].StartsWith('-')
                            ? args[++i]
                            : null;

                        if (optValue == null)
                        {
                            result.success = false;
                            result.error_message = $"选项 --{optName} 需要指定值";
                            return result;
                        }

                        set_property_value(model, matched, optValue);
                    }
                }
                else
                {
                    result.success = false;
                    result.error_message = $"未知选项: --{optName}";
                    return result;
                }
            }
            else if (arg.StartsWith('-') && arg.Length == 2 && char.IsLetter(arg[1]))
            {
                var shortName = arg[1];
                var matched = optionProperties.FirstOrDefault(p =>
                {
                    var opt = p.GetCustomAttribute<OptionAttribute>()!;
                    return opt.short_name == shortName;
                });

                if (matched != null)
                {
                    var propType = matched.PropertyType;
                    if (propType == typeof(bool))
                    {
                        matched.SetValue(model, true);
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            result.success = false;
                            result.error_message = $"选项 -{shortName} 需要指定值";
                            return result;
                        }

                        set_property_value(model, matched, args[++i]);
                    }
                }
                else
                {
                    result.success = false;
                    result.error_message = $"未知选项: -{shortName}";
                    return result;
                }
            }
            else if (subCommandProperties.Count > 0)
            {
                var subProp = find_sub_command_property(subCommandProperties, type, arg);
                if (subProp != null)
                {
                    var subType = subProp.PropertyType;
                    var parseMethod = typeof(CliArgumentParser).GetMethod(nameof(parse), 1, [typeof(string[])])!
                        .MakeGenericMethod(subType);
                    var subResult = parseMethod.Invoke(null, [args[(i + 1)..]]);

                    var subModelProp = subResult!.GetType().GetProperty("model");
                    var subModel = subModelProp!.GetValue(subResult);
                    subProp.SetValue(model, subModel);

                    result.sub_command_name = arg;
                    result.success = true;
                    result.model = model;
                    return result;
                }

                if (argIndex < argProperties.Count && !hasRemainingArg)
                {
                    set_property_value(model, argProperties[argIndex], arg);
                    argIndex++;
                }
                else if (hasRemainingArg)
                {
                    append_remaining_arg(model, argProperties[^1], [arg]);
                }
            }
            else if (hasRemainingArg && argIndex >= argProperties.Count - 1)
            {
                append_remaining_arg(model, argProperties[^1], args[i..]);
                break;
            }
            else if (argIndex < argProperties.Count)
            {
                set_property_value(model, argProperties[argIndex], arg);
                argIndex++;
            }

            i++;
        }

        for (var aIdx = argIndex; aIdx < (hasRemainingArg ? argProperties.Count - 1 : argProperties.Count); aIdx++)
        {
            var attr = argProperties[aIdx].GetCustomAttribute<ArgumentAttribute>()!;
            if (attr.required)
            {
                result.success = false;
                result.error_message = $"缺少必填参数: {argProperties[aIdx].Name}";
                return result;
            }
        }

        apply_environment_variables(model, optionProperties);

        var validationError = validate_model(model, optionProperties);
        if (validationError is not null)
        {
            result.success = false;
            result.error_message = validationError;
            return result;
        }

        result.success = true;
        result.model = model;
        return result;
    }

    internal static int parse_and_execute(BuiltCommandModel model, string[] args, string[]? commandPath = null)
    {
        commandPath ??= [model.name];
        var optionValues = new Dictionary<string, object?>();
        var argValues = new List<object?>();
        var argIndex = 0;

        var remaining = new List<string>();

        var i = 0;
        while (i < args.Length)
        {
            var arg = args[i];

            if (arg is "--help" or "-h")
            {
                print_help(model, commandPath);
                return 0;
            }

            if (arg == "--")
            {
                i++;
                while (i < args.Length)
                {
                    remaining.Add(args[i]);
                    i++;
                }

                break;
            }

            if (arg.StartsWith("--"))
            {
                var eqIndex = arg.IndexOf('=');
                string optName;
                string? optValue;

                if (eqIndex >= 0)
                {
                    optName = arg[2..eqIndex];
                    optValue = arg[(eqIndex + 1)..];
                }
                else
                {
                    optName = arg[2..];
                    optValue = null;
                }

                var optDef = find_option_def(model.options, optName);
                if (optDef != null)
                {
                    if (optDef.is_flag)
                    {
                        optionValues[optDef.name] = true;
                    }
                    else
                    {
                        optValue ??= i + 1 < args.Length && !args[i + 1].StartsWith('-')
                            ? args[++i]
                            : null;

                        if (optValue == null)
                        {
                            System.Console.Error.WriteLine($"选项 --{optName} 需要指定值");
                            return 1;
                        }

                        optionValues[optDef.name] = convert_value(optValue, optDef.type);
                    }
                }
                else
                {
                    System.Console.Error.WriteLine($"未知选项: --{optName}");
                    return 1;
                }
            }
            else if (arg.StartsWith('-') && arg.Length == 2 && char.IsLetter(arg[1]))
            {
                var shortName = arg[1];
                var optDef = model.options.FirstOrDefault(o => o.short_name == shortName);

                if (optDef != null)
                {
                    if (optDef.is_flag)
                    {
                        optionValues[optDef.name] = true;
                    }
                    else
                    {
                        if (i + 1 >= args.Length)
                        {
                            System.Console.Error.WriteLine($"选项 -{shortName} 需要指定值");
                            return 1;
                        }

                        optionValues[optDef.name] = convert_value(args[++i], optDef.type);
                    }
                }
                else
                {
                    System.Console.Error.WriteLine($"未知选项: -{shortName}");
                    return 1;
                }
            }
            else
            {
                var subCmd = model.sub_commands.FirstOrDefault(s =>
                    string.Equals(s.name, arg, StringComparison.OrdinalIgnoreCase));

                if (subCmd != null)
                {
                    var nestedPath = new string[commandPath.Length + 1];
                    commandPath.CopyTo(nestedPath, 0);
                    nestedPath[^1] = subCmd.name;
                    return parse_and_execute(subCmd, args[(i + 1)..], nestedPath);
                }

                remaining.Add(arg);
            }

            i++;
        }

        var hasCatchAllArg = model.arguments.Count > 0
                             && is_remaining_args_type(model.arguments[^1].type);

        foreach (var val in remaining)
        {
            if (argIndex < model.arguments.Count)
            {
                if (hasCatchAllArg && argIndex == model.arguments.Count - 1)
                    argValues.Add(convert_value(val, model.arguments[^1].type));
                else
                    argValues.Add(convert_value(val, model.arguments[argIndex].type));
            }
            else if (hasCatchAllArg)
            {
                argValues.Add(convert_value(val, typeof(string)));
            }

            argIndex++;
        }

        for (var aIdx = 0; aIdx < (hasCatchAllArg ? model.arguments.Count - 1 : model.arguments.Count); aIdx++)
        {
            var aDef = model.arguments[aIdx];
            if (aIdx < argValues.Count) continue;

            if (aDef.default_value != null)
            {
                argValues.Insert(aIdx, aDef.default_value);
            }
            else if (aDef.required)
            {
                System.Console.Error.WriteLine($"缺少必填参数: {aDef.name}");
                return 1;
            }
            else
            {
                argValues.Insert(aIdx, get_default_for_type(aDef.type));
            }
        }

        foreach (var optDef in model.options)
            if (!optionValues.ContainsKey(optDef.name))
                optionValues[optDef.name] = optDef.default_value ?? get_default_for_type(optDef.type);

        apply_environment_variables_for_builder(optionValues, model.options);

        var builderValidationError = validate_builder_options(optionValues, model.options);
        if (builderValidationError is not null)
        {
            System.Console.Error.WriteLine(builderValidationError);
            return 1;
        }

        if (model.handler != null) return invoke_handler(model.handler, argValues, optionValues, model);

        if (model.handler_type != null)
        {
            var instance = Activator.CreateInstance(model.handler_type);
            var method = model.handler_type.GetMethod("ExecuteAsync")
                         ?? model.handler_type.GetMethod("Invoke")
                         ?? throw new InvalidOperationException($"处理程序 {model.handler_type.Name} 中未找到可执行方法");

            var parameters = build_handler_parameters(method, argValues, optionValues, model);
            var handlerResult = method.Invoke(instance, parameters);

            if (handlerResult is Task<int> task)
            {
                task.Wait();
                return task.Result;
            }

            if (handlerResult is int exitCode) return exitCode;

            return 0;
        }

        System.Console.Error.WriteLine($"命令 {model.name} 未绑定处理程序");
        return 1;
    }

    private static OptionDef? find_option_def(List<OptionDef> options, string name)
    {
        return options.FirstOrDefault(o =>
            string.Equals(o.name, name, StringComparison.OrdinalIgnoreCase)
            || o.aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));
    }

    private static int invoke_handler(Delegate handler, List<object?> argValues,
        Dictionary<string, object?> optionValues, BuiltCommandModel model)
    {
        var parameters = build_handler_parameters(handler.Method, argValues, optionValues, model);
        var result = handler.DynamicInvoke(parameters);

        if (result is Task<int> task)
        {
            task.Wait();
            return task.Result;
        }

        if (result is int exitCode) return exitCode;

        return 0;
    }

    private static object?[] build_handler_parameters(MethodInfo method, List<object?> argValues,
        Dictionary<string, object?> optionValues, BuiltCommandModel model)
    {
        var methodParams = method.GetParameters();
        var parameters = new object?[methodParams.Length];

        var argIdx = 0;
        var hasCatchAllArg = model.arguments.Count > 0
                             && is_remaining_args_type(model.arguments[^1].type);

        for (var pIdx = 0; pIdx < methodParams.Length; pIdx++)
        {
            var param = methodParams[pIdx];

            if (model.options.Any(o => string.Equals(o.name, param.Name, StringComparison.OrdinalIgnoreCase)))
            {
                parameters[pIdx] = optionValues.TryGetValue(param.Name!, out var val)
                    ? val
                    : get_default_for_type(param.ParameterType);
            }
            else if (hasCatchAllArg && pIdx == methodParams.Length - 1)
            {
                parameters[pIdx] = argValues.Skip(argIdx).ToArray();
                argIdx = argValues.Count;
            }
            else if (argIdx < argValues.Count)
            {
                parameters[pIdx] = argValues[argIdx];
                argIdx++;
            }
            else
            {
                parameters[pIdx] = get_default_for_type(param.ParameterType);
            }
        }

        return parameters;
    }

    private static object? convert_value(string value, Type targetType)
    {
        try
        {
            return TypeConverterRegistry.@default.convert(value, targetType);
        }
        catch (FormatException)
        {
            throw new FormatException($"无法将 \"{value}\" 转换为类型 {targetType.Name}");
        }
    }

    private static object? get_default_for_type(Type type)
    {
        if (type.IsValueType) return Activator.CreateInstance(type);

        return null;
    }

    private static bool is_remaining_args_type(Type type)
    {
        return type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
    }

    private static void append_remaining_arg<T>(T model, PropertyInfo prop, string[] values) where T : new()
    {
        var elementType = prop.PropertyType.IsArray
            ? prop.PropertyType.GetElementType()!
            : prop.PropertyType.GetGenericArguments()[0];

        var converted = values.Select(v => convert_value(v, elementType)).ToArray();

        if (prop.PropertyType.IsArray)
        {
            var arr = Array.CreateInstance(elementType, converted.Length);
            Array.Copy(converted, arr, converted.Length);
            prop.SetValue(model, arr);
        }
        else
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add")!;
            foreach (var item in converted) addMethod.Invoke(list, [item]);

            prop.SetValue(model, list);
        }
    }

    private static PropertyInfo? find_option_property(List<PropertyInfo> properties, string name)
    {
        return properties.FirstOrDefault(p =>
        {
            var opt = p.GetCustomAttribute<OptionAttribute>()!;
            return string.Equals(opt.long_name, name, StringComparison.OrdinalIgnoreCase)
                   || (opt.short_name.HasValue && opt.short_name.Value.ToString() == name);
        });
    }

    private static PropertyInfo? find_sub_command_property(List<PropertyInfo> properties, Type parentType, string name)
    {
        foreach (var prop in properties)
        {
            var subType = prop.PropertyType;
            var cmdAttr = subType.GetCustomAttribute<CommandAttribute>();
            if (cmdAttr != null)
                if (string.Equals(cmdAttr.name, name, StringComparison.OrdinalIgnoreCase)
                    || cmdAttr.alias.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)))
                    return prop;
        }

        return null;
    }

    private static void set_property_value<T>(T model, PropertyInfo prop, string value) where T : new()
    {
        var propType = prop.PropertyType;
        var converted = convert_value(value, propType);
        prop.SetValue(model, converted);
    }

    private static void print_help(BuiltCommandModel model, string[] commandPath)
    {
        var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "app";
        var helpText = HelpGenerator.generate_command_help(appName, model, commandPath);
        System.Console.WriteLine(helpText);
    }

    private static void apply_environment_variables<T>(T model, List<PropertyInfo> optionProperties) where T : new()
    {
        foreach (var prop in optionProperties)
        {
            var optAttr = prop.GetCustomAttribute<OptionAttribute>()!;
            var currentValue = prop.GetValue(model);
            var propType = prop.PropertyType;
            var isDefault = propType.IsValueType
                ? Equals(currentValue, Activator.CreateInstance(propType))
                : currentValue is null;

            if (!isDefault) continue;

            if (!string.IsNullOrEmpty(optAttr.environment_variable))
            {
                var envValue = Environment.GetEnvironmentVariable(optAttr.environment_variable);
                if (!string.IsNullOrEmpty(envValue))
                {
                    set_property_value(model, prop, envValue);
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(optAttr.config_key))
            {
                var config = CommandApp.get_configuration();
                if (config != null)
                {
                    var configValue = config.get(optAttr.config_key);
                    if (!string.IsNullOrEmpty(configValue)) set_property_value(model, prop, configValue);
                }
            }
        }
    }

    private static string? validate_model<T>(T model, List<PropertyInfo> optionProperties) where T : new()
    {
        foreach (var prop in optionProperties)
        {
            var rangeAttr = prop.GetCustomAttribute<RangeAttribute>();
            if (rangeAttr is null) continue;

            var value = prop.GetValue(model);
            if (value is null) continue;

            var comparable = value as IComparable;
            if (comparable is null) continue;

            if (comparable.CompareTo(rangeAttr.min) < 0)
                return
                    $"选项 --{prop.GetCustomAttribute<OptionAttribute>()!.long_name ?? prop.Name} 的值 {value} 小于最小值 {rangeAttr.min}";

            if (comparable.CompareTo(rangeAttr.max) > 0)
                return
                    $"选项 --{prop.GetCustomAttribute<OptionAttribute>()!.long_name ?? prop.Name} 的值 {value} 大于最大值 {rangeAttr.max}";
        }

        return null;
    }

    private static void apply_environment_variables_for_builder(Dictionary<string, object?> optionValues,
        List<OptionDef> options)
    {
        foreach (var optDef in options)
        {
            var currentValue = optionValues.GetValueOrDefault(optDef.name);
            var isDefault = currentValue is null ||
                            (optDef.type.IsValueType && Equals(currentValue, Activator.CreateInstance(optDef.type)));

            if (!isDefault) continue;

            if (!string.IsNullOrEmpty(optDef.environment_variable))
            {
                var envValue = Environment.GetEnvironmentVariable(optDef.environment_variable);
                if (!string.IsNullOrEmpty(envValue))
                {
                    optionValues[optDef.name] = convert_value(envValue, optDef.type);
                    continue;
                }
            }

            if (!string.IsNullOrEmpty(optDef.config_key))
            {
                var config = CommandApp.get_configuration();
                if (config != null)
                {
                    var configValue = config.get(optDef.config_key);
                    if (!string.IsNullOrEmpty(configValue))
                        optionValues[optDef.name] = convert_value(configValue, optDef.type);
                }
            }
        }
    }

    private static string? validate_builder_options(Dictionary<string, object?> optionValues, List<OptionDef> options)
    {
        foreach (var optDef in options)
        {
            if (optDef.minimum is null && optDef.maximum is null) continue;

            var value = optionValues.GetValueOrDefault(optDef.name);
            if (value is null) continue;

            var comparable = value as IComparable;
            if (comparable is null) continue;

            if (optDef.minimum is not null && comparable.CompareTo(optDef.minimum) < 0)
                return $"选项 --{optDef.name} 的值 {value} 小于最小值 {optDef.minimum}";

            if (optDef.maximum is not null && comparable.CompareTo(optDef.maximum) > 0)
                return $"选项 --{optDef.name} 的值 {value} 大于最大值 {optDef.maximum}";
        }

        return null;
    }
}