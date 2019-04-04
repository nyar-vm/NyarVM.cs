namespace Std.Terminal;

public sealed class ScreenBuffer
{
    public static readonly RgbColor default_foreground = RgbColor.white;
    public static readonly RgbColor default_background = RgbColor.black;
    private readonly RgbColor[,] _background_colors;
    private readonly char[,] _chars;
    private readonly RgbColor[,] _foreground_colors;

    public ScreenBuffer(int width, int height)
    {
        this.width = width;
        this.height = height;
        _chars = new char[height, width];
        _foreground_colors = new RgbColor[height, width];
        _background_colors = new RgbColor[height, width];
        clear();
    }

    public int width { get; }
    public int height { get; }

    public void clear()
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            _chars[y, x] = ' ';
            _foreground_colors[y, x] = default_foreground;
            _background_colors[y, x] = default_background;
        }
    }

    public void set_char(int x, int y, char c, RgbColor foreground, RgbColor background)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;

        _chars[y, x] = c;
        _foreground_colors[y, x] = foreground;
        _background_colors[y, x] = background;
    }

    public void set_string(int x, int y, string text, RgbColor foreground, RgbColor background)
    {
        for (var i = 0; i < text.Length; i++) set_char(x + i, y, text[i], foreground, background);
    }

    internal char get_char(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return ' ';

        return _chars[y, x];
    }

    internal RgbColor get_foreground(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return default_foreground;

        return _foreground_colors[y, x];
    }

    internal RgbColor get_background(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return default_background;

        return _background_colors[y, x];
    }

    internal bool cell_equals(ScreenBuffer other, int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;

        return _chars[y, x] == other._chars[y, x]
               && _foreground_colors[y, x].Equals(other._foreground_colors[y, x])
               && _background_colors[y, x].Equals(other._background_colors[y, x]);
    }
}