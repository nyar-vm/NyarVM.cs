using System.Runtime.InteropServices;
using Core.Console;

namespace Std.Console;

/// <summary>
///     封装 <c>System.Console</c> 的默认控制台实现，支持原始模式和终端能力探测�?///
/// </summary>
public sealed class SystemConsoleWrapper : IConsole
{
    private bool _in_raw_mode;
    private int _original_input_mode;
    private int _original_output_mode;
    private IntPtr _original_termios;
    private bool _saved_original_mode;

    private bool? _supports_ansi;
    private bool? _supports_hyperlinks;
    private bool? _supports_true_color;

    TextReader IConsole.@in => System.Console.In;

    TextWriter IConsole.@out => System.Console.Out;

    TextWriter IConsole.error => System.Console.Error;

    bool IConsole.is_input_redirected => System.Console.IsInputRedirected;

    bool IConsole.is_output_redirected => System.Console.IsOutputRedirected;

    bool IConsole.is_error_redirected => System.Console.IsErrorRedirected;

    ConsoleColor IConsole.foreground_color
    {
        get => System.Console.ForegroundColor;
        set => System.Console.ForegroundColor = value;
    }

    ConsoleColor IConsole.background_color
    {
        get => System.Console.BackgroundColor;
        set => System.Console.BackgroundColor = value;
    }

    void IConsole.reset_color()
    {
        System.Console.ResetColor();
    }

    int IConsole.cursor_left
    {
        get => System.Console.CursorLeft;
        set => System.Console.CursorLeft = value;
    }

    int IConsole.cursor_top
    {
        get => System.Console.CursorTop;
        set => System.Console.CursorTop = value;
    }

    int IConsole.buffer_width => System.Console.BufferWidth;

    int IConsole.buffer_height => System.Console.BufferHeight;

    int IConsole.window_width => System.Console.WindowWidth;

    int IConsole.window_height => System.Console.WindowHeight;

    void IConsole.set_cursor_position(int left, int top)
    {
        System.Console.SetCursorPosition(left, top);
    }

    void IConsole.clear()
    {
        System.Console.Clear();
    }

    void IConsole.beep()
    {
        System.Console.Beep();
    }

    void IConsole.enter_raw_mode()
    {
        if (_in_raw_mode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            enter_raw_mode_windows();
        else
            enter_raw_mode_unix();

        _in_raw_mode = true;
    }

    void IConsole.exit_raw_mode()
    {
        if (!_in_raw_mode) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            exit_raw_mode_windows();
        else
            exit_raw_mode_unix();

        _in_raw_mode = false;
    }

    event ConsoleCancelEventHandler? IConsole.CancelKeyPress
    {
        add => System.Console.CancelKeyPress += value;
        remove => System.Console.CancelKeyPress -= value;
    }

    bool IConsole.supports_ansi => _supports_ansi ??= detect_ansi_support();

    bool IConsole.supports_true_color => _supports_true_color ??= detect_true_color_support();

    bool IConsole.supports_hyperlinks => _supports_hyperlinks ??= detect_hyperlink_support();

    #region Windows 原始模式

    private void enter_raw_mode_windows()
    {
        var inputHandle = GetStdHandle(_std_input_handle);
        var outputHandle = GetStdHandle(_std_output_handle);

        if (GetConsoleMode(inputHandle, out var inputMode))
        {
            _original_input_mode = inputMode;
            _saved_original_mode = true;
            var newMode = inputMode & ~(_enable_line_input | _enable_echo_input | _enable_processed_input);
            SetConsoleMode(inputHandle, newMode);
        }

        if (GetConsoleMode(outputHandle, out var outputMode))
        {
            _original_output_mode = outputMode;
            SetConsoleMode(outputHandle, outputMode | _enable_virtual_terminal_processing);
        }
    }

    private void exit_raw_mode_windows()
    {
        if (_saved_original_mode)
        {
            var inputHandle = GetStdHandle(_std_input_handle);
            SetConsoleMode(inputHandle, _original_input_mode);
            _saved_original_mode = false;
        }

        var outputHandle = GetStdHandle(_std_output_handle);
        if (GetConsoleMode(outputHandle, out var outputMode)) SetConsoleMode(outputHandle, _original_output_mode);
    }

    #endregion

    #region Unix 原始模式

    private void enter_raw_mode_unix()
    {
        var fd = _stdin_file_descriptor;
        if (tcgetattr(fd, out var termios) == 0)
        {
            _original_termios = Marshal.AllocHGlobal(Marshal.SizeOf<TermiosT>());
            Marshal.StructureToPtr(termios, _original_termios, false);

            termios.c_lflag &= ~(_icanon | _echo | _isig);
            termios.c_cc[_vtime] = 0;
            termios.c_cc[_vmin] = 1;
            tcsetattr(fd, _tcsaflush, ref termios);
        }
    }

    private void exit_raw_mode_unix()
    {
        if (_original_termios != IntPtr.Zero)
        {
            var fd = _stdin_file_descriptor;
            var original = Marshal.PtrToStructure<TermiosT>(_original_termios);
            tcsetattr(fd, _tcsaflush, ref original);
            Marshal.FreeHGlobal(_original_termios);
            _original_termios = IntPtr.Zero;
        }
    }

    #endregion

    #region 终端能力探测

    private static bool detect_ansi_support()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetStdHandle(_std_output_handle);
            if (GetConsoleMode(handle, out var mode))
                return (mode & _enable_virtual_terminal_processing) != 0 ||
                       try_enable_virtual_terminal_processing(handle, mode);

            return false;
        }

        var term = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(term)) return false;

        return term != "dumb";
    }

    private static bool try_enable_virtual_terminal_processing(IntPtr handle, int mode)
    {
        try
        {
            return SetConsoleMode(handle, mode | _enable_virtual_terminal_processing);
        }
        catch
        {
            return false;
        }
    }

    private static bool detect_true_color_support()
    {
        var colorterm = Environment.GetEnvironmentVariable("COLORTERM");
        return colorterm is "truecolor" or "24bit" or "yes";
    }

    private static bool detect_hyperlink_support()
    {
        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        return termProgram is "iTerm.app" or "WezTerm" or "Hyper" or "vscode";
    }

    #endregion

    #region Windows P/Invoke

    private const int _std_input_handle = -10;
    private const int _std_output_handle = -11;
    private const int _enable_line_input = 0x0002;
    private const int _enable_echo_input = 0x0004;
    private const int _enable_processed_input = 0x0001;
    private const int _enable_virtual_terminal_processing = 0x0004;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

    #endregion

    #region Unix P/Invoke

    private const int _stdin_file_descriptor = 0;
    private const uint _icanon = 0x00000100;
    private const uint _echo = 0x00000008;
    private const uint _isig = 0x00000080;
    private const int _vmin = 6;
    private const int _vtime = 5;
    private const int _tcsaflush = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct TermiosT
    {
        public uint c_iflag;
        public uint c_oflag;
        public uint c_cflag;
        public uint c_lflag;
        public byte c_line;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] c_cc;
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int tcgetattr(int fd, out TermiosT termios);

    [DllImport("libc", SetLastError = true)]
    private static extern int tcsetattr(int fd, int optionalActions, ref TermiosT termios);

    #endregion
}