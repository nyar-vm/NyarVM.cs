using Core.Console;
using Std.Category;
using Std.Text.Utf8;

namespace Std.Console;

/// <summary>平台无关的控制台输入输出</summary>
public static class SonicConsole
{
    private static IConsoleAdapter _adapter = new ClrConsoleAdapter();

    /// <summary>设置控制台适配器，用于不同平台�?I/O 实现</summary>
    public static void set_adapter(IConsoleAdapter adapter)
    {
        _adapter = adapter;
    }

    /// <summary>打印字符串到控制?/summary>
    public static void print(Utf8Text text)
    {
        _adapter.write(text.ToString());
    }

    /// <summary>打印字符串并追加换行</summary>
    public static void print_line(Utf8Text text)
    {
        _adapter.write_line(text.ToString());
    }

    /// <summary>打印换行?/summary>
    public static void print_line()
    {
        _adapter.write_line(Utf8Text.empty.ToString());
    }

    /// <summary>打印错误信息到标准错�?/summary>
    public static void error(Utf8Text text)
    {
        System.Console.Error.Write(text.ToString());
    }

    /// <summary>打印调试信息到标准错�?/summary>
    public static void debug(Utf8Text text)
    {
        System.Console.Error.Write($"[DEBUG] {text}\n");
    }

    /// <summary>格式化打?/summary>
    public static void print_fmt(string fmt, params object[] args)
    {
        var result = Utf8Text.from_string(string.Format(fmt, args));
        _adapter.write(result.ToString());
    }

    /// <summary>
    ///     读取一行输�?/summary>
    ///     <returns>用户输入的文本行</returns>
    public static Utf8Text read_line()
    {
        return _adapter.read_line();
    }

    /// <summary>读取全部输入直到 EOF</summary>
    /// <returns>全部输入文本</returns>
    public static Utf8Text read_all()
    {
        return _adapter.read_all();
    }

    /// <summary>
    ///     读取一个字�?/summary>
    ///     <returns>读取到的字符或不包含值的 <see cref="Option{T}" /></returns>
    public static Option<char> read_char()
    {
        var line = read_line();
        if (!line.is_empty)
        {
            var bytes = new byte[line.byte_length];
            line.as_span().CopyTo(bytes);
            return Option<char>.some((char)bytes[0]);
        }

        return Option<char>.none;
    }

    /// <summary>读取一�?I32 整数</summary>
    /// <returns>读取到的整数或不包含值的 <see cref="Option{T}" /></returns>
    public static Option<int> read_i32()
    {
        var line = read_line();
        if (int.TryParse(line.ToString(), out var value)) return Option<int>.some(value);

        return Option<int>.none;
    }

    /// <summary>
    ///     读取一�?F64 浮点�?/summary>
    ///     <returns>读取到的浮点数或不包含值的 <see cref="Option{T}" /></returns>
    public static Option<double> read_f64()
    {
        var line = read_line();
        if (double.TryParse(line.ToString(), out var value)) return Option<double>.some(value);

        return Option<double>.none;
    }

    private sealed class ClrConsoleAdapter : IConsoleAdapter
    {
        void IConsoleAdapter.write(string text)
        {
            System.Console.Write(text);
        }

        void IConsoleAdapter.write_line(string text)
        {
            System.Console.WriteLine(text);
        }

        string IConsoleAdapter.read_line()
        {
            return System.Console.ReadLine() ?? "";
        }

        string IConsoleAdapter.read_all()
        {
            return System.Console.In.ReadToEnd();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void write(Utf8Text text)
        {
            System.Console.Write(text.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void write_line(Utf8Text text)
        {
            System.Console.WriteLine(text.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Text read_line()
        {
            var line = System.Console.ReadLine();
            return Utf8Text.from_string(line ?? "");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Utf8Text read_all()
        {
            var text = System.Console.In.ReadToEnd();
            return Utf8Text.from_string(text);
        }
    }
}