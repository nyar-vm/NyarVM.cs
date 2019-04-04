using Nyar.Language;
using Nyar.VM.LegacyVM.Algebra;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     语言运行时配置工厂，为每种语言提供预配置的 LanguageRuntimeConfig
/// </summary>
public static class LanguageRuntimeConfigFactory
{
    /// <summary>
    ///     创建 Python 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_python_config()
    {
        var builtins = new Dictionary<string, Func<object[], object>>
        {
            ["len"] = args =>
            {
                if (args.Length == 0)
                {
                    return 0L;
                }

                var arg = args[0];
                if (arg is string s)
                {
                    return (long)s.Length;
                }

                if (arg is System.Collections.IList list)
                {
                    return (long)list.Count;
                }

                return 0L;
            },
            ["type"] = args =>
            {
                if (args.Length == 0)
                {
                    return "NoneType";
                }

                return args[0] switch
                {
                    null => "NoneType",
                    long => "int",
                    bool => "bool",
                    string => "str",
                    double => "float",
                    System.Collections.IList => "list",
                    Dictionary<string, object> => "dict",
                    LambdaClosure => "function",
                    BuiltinFunction => "builtin_function",
                    _ => args[0].GetType().Name.ToLower()
                };
            },
            ["str"] = args => args.Length == 0 ? "" : CoreHelpers.to_str(args[0]),
            ["int"] = args => args.Length == 0 ? 0L : CoreHelpers.to_i64(args[0]),
            ["float"] = args => args.Length == 0 ? 0.0 : CoreHelpers.to_f64(args[0]),
            ["range"] = args =>
            {
                long start = 0, end, step = 1;
                if (args.Length == 1)
                {
                    end = CoreHelpers.to_i64(args[0]);
                }
                else if (args.Length >= 2)
                {
                    start = CoreHelpers.to_i64(args[0]);
                    end = CoreHelpers.to_i64(args[1]);
                    if (args.Length >= 3)
                    {
                        step = CoreHelpers.to_i64(args[2]);
                    }
                }
                else
                {
                    return new List<object>();
                }

                var result = new List<object>();
                for (var v = start; v < end; v += step)
                {
                    result.Add(v);
                }

                return result;
            },
            ["abs"] = args => args.Length == 0 ? 0L : Math.Abs(CoreHelpers.to_i64(args[0])),
            ["min"] = args =>
            {
                if (args.Length == 0)
                {
                    return 0L;
                }

                if (args is [System.Collections.IList list])
                {
                    if (list.Count == 0)
                    {
                        return 0L;
                    }

                    var minVal = CoreHelpers.to_i64(list[0] ?? 0L);
                    foreach (var item in list)
                    {
                        var v = CoreHelpers.to_i64(item ?? 0L);
                        if (v < minVal)
                        {
                            minVal = v;
                        }
                    }

                    return minVal;
                }

                var result = CoreHelpers.to_i64(args[0]);
                for (var i = 1; i < args.Length; i++)
                {
                    var v = CoreHelpers.to_i64(args[i]);
                    if (v < result)
                    {
                        result = v;
                    }
                }

                return result;
            },
            ["max"] = args =>
            {
                if (args.Length == 0)
                {
                    return 0L;
                }

                if (args is [System.Collections.IList list])
                {
                    if (list.Count == 0)
                    {
                        return 0L;
                    }

                    var maxVal = CoreHelpers.to_i64(list[0] ?? 0L);
                    foreach (var item in list)
                    {
                        var v = CoreHelpers.to_i64(item ?? 0L);
                        if (v > maxVal)
                        {
                            maxVal = v;
                        }
                    }

                    return maxVal;
                }

                var result = CoreHelpers.to_i64(args[0]);
                for (var i = 1; i < args.Length; i++)
                {
                    var v = CoreHelpers.to_i64(args[i]);
                    if (v > result)
                    {
                        result = v;
                    }
                }

                return result;
            },
            ["isinstance"] = args => args.Length >= 2
        };

        return LanguageRuntimeConfig.create(DialectTarget.Standard, builtins);
    }

    /// <summary>
    ///     创建 JavaScript 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_javascript_config()
    {
        var builtins = new Dictionary<string, Func<object[], object>>
        {
            ["console.log"] = args =>
            {
                foreach (var a in args)
                {
                    Console.WriteLine(CoreHelpers.to_str(a));
                }

                return new object();
            },
            ["parseInt"] = args => args.Length == 0 ? 0L : CoreHelpers.to_i64(args[0]),
            ["parseFloat"] = args => args.Length == 0 ? 0.0 : CoreHelpers.to_f64(args[0]),
            ["String"] = args => args.Length == 0 ? "" : CoreHelpers.to_str(args[0]),
            ["Number"] = args => args.Length == 0 ? 0L : CoreHelpers.to_i64(args[0]),
            ["Boolean"] = args => args.Length != 0 && CoreHelpers.to_bool(args[0]),
            ["Math.abs"] = args => args.Length == 0 ? 0L : Math.Abs(CoreHelpers.to_i64(args[0])),
            ["Math.min"] = args => args.Length == 0 ? 0L : args.Min(CoreHelpers.to_i64),
            ["Math.max"] = args => args.Length == 0 ? 0L : args.Max(CoreHelpers.to_i64),
            ["Math.floor"] = args => args.Length == 0 ? 0L : (long)Math.Floor(CoreHelpers.to_f64(args[0])),
            ["Math.ceil"] = args => args.Length == 0 ? 0L : (long)Math.Ceiling(CoreHelpers.to_f64(args[0])),
            ["Math.round"] = args => args.Length == 0 ? 0L : (long)Math.Round(CoreHelpers.to_f64(args[0])),
            ["Math.sqrt"] = args => args.Length == 0 ? 0.0 : Math.Sqrt(CoreHelpers.to_f64(args[0])),
            ["Math.pow"] = args =>
                args.Length < 2 ? 0.0 : Math.Pow(CoreHelpers.to_f64(args[0]), CoreHelpers.to_f64(args[1])),
            ["Array.isArray"] = args => args.Length > 0 && args[0] is System.Collections.IList,
            ["Object.keys"] = args =>
            {
                if (args.Length == 0 || args[0] is not Dictionary<string, object> dict)
                {
                    return new List<object>();
                }

                return new List<object>(dict.Keys.Cast<object>());
            }
        };

        return LanguageRuntimeConfig.create(DialectTarget.Standard, builtins);
    }

    /// <summary>
    ///     创建 TypeScript 运行时配置（与 JavaScript 共享内置函数）
    /// </summary>
    public static LanguageRuntimeConfig create_typescript_config()
    {
        return create_javascript_config();
    }

    /// <summary>
    ///     创建 Lua 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_lua_config()
    {
        var builtins = new Dictionary<string, Func<object[], object>>
        {
            ["print"] = args =>
            {
                foreach (var a in args)
                {
                    Console.Write(CoreHelpers.to_str(a));
                }

                Console.WriteLine();
                return new object();
            },
            ["type"] = args =>
            {
                if (args.Length == 0)
                {
                    return "nil";
                }

                return args[0] switch
                {
                    null => "nil",
                    string => "string",
                    long or double => "number",
                    bool => "boolean",
                    _ => "table"
                };
            },
            ["tostring"] = args => args.Length == 0 ? "nil" : CoreHelpers.to_str(args[0]),
            ["tonumber"] = args =>
            {
                if (args.Length == 0)
                {
                    return null!;
                }

                var arg = args[0];
                if (arg is long or double)
                {
                    return arg;
                }

                var s = CoreHelpers.to_str(arg);
                if (long.TryParse(s, out var l))
                {
                    return l;
                }

                if (double.TryParse(s, out var d))
                {
                    return d;
                }

                return null!;
            },
            ["pairs"] = args => args,
            ["ipairs"] = args => args
        };

        return LanguageRuntimeConfig.create(DialectTarget.Standard, builtins);
    }

    /// <summary>
    ///     创建 Julia 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_julia_config()
    {
        var builtins = new Dictionary<string, Func<object[], object>>
        {
            ["println"] = args =>
            {
                foreach (var a in args)
                {
                    Console.WriteLine(CoreHelpers.to_str(a));
                }

                return new object();
            },
            ["print"] = args =>
            {
                foreach (var a in args)
                {
                    Console.Write(CoreHelpers.to_str(a));
                }

                return new object();
            },
            ["length"] = args =>
            {
                if (args.Length == 0)
                {
                    return 0L;
                }

                var arg = args[0];
                if (arg is string s)
                {
                    return (long)s.Length;
                }

                if (arg is System.Collections.IList list)
                {
                    return (long)list.Count;
                }

                return 0L;
            },
            ["typeof"] = args =>
            {
                if (args.Length == 0)
                {
                    return "Nothing";
                }

                return args[0] switch
                {
                    null => "Nothing",
                    long => "Int64",
                    double => "Float64",
                    bool => "Bool",
                    string => "String",
                    _ => args[0].GetType().Name
                };
            },
            ["string"] = args => args.Length == 0 ? "" : CoreHelpers.to_str(args[0]),
            ["Int"] = args => args.Length == 0 ? 0L : CoreHelpers.to_i64(args[0]),
            ["Float64"] = args => args.Length == 0 ? 0.0 : CoreHelpers.to_f64(args[0]),
            ["abs"] = args => args.Length == 0 ? 0L : Math.Abs(CoreHelpers.to_i64(args[0])),
            ["min"] = args => args.Length == 0 ? 0L : args.Min(CoreHelpers.to_i64),
            ["max"] = args => args.Length == 0 ? 0L : args.Max(CoreHelpers.to_i64),
            ["sqrt"] = args => args.Length == 0 ? 0.0 : Math.Sqrt(CoreHelpers.to_f64(args[0]))
        };

        return LanguageRuntimeConfig.create(DialectTarget.Standard, builtins);
    }

    /// <summary>
    ///     创建 SQL 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_sql_config()
    {
        return LanguageRuntimeConfig.create(DialectTarget.Standard);
    }

    /// <summary>
    ///     创建 C 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_c_config()
    {
        return LanguageRuntimeConfig.create(DialectTarget.Core);
    }

    /// <summary>
    ///     创建 Rust 运行时配置
    /// </summary>
    public static LanguageRuntimeConfig create_rust_config()
    {
        return LanguageRuntimeConfig.create(DialectTarget.Core);
    }

    /// <summary>
    ///     创建 Shell 运行时配置（Bash、Batch、PowerShell 均使用 Standard 方言）
    /// </summary>
    public static LanguageRuntimeConfig create_shell_config(string? variablePrefix = null)
    {
        return new LanguageRuntimeConfig
        {
            dialect = DialectTarget.Standard,
            variable_prefix = variablePrefix
        };
    }
}