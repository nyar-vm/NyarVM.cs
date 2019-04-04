namespace Sonic.Interactive;

/// <summary>
/// REPL 命令历史管理器，支持持久化存储
/// </summary>
public sealed class ReplHistory
{
    private readonly List<string> _entries = [];
    private readonly int _max_entries;

    /// <summary>
    /// 所有历史条目
    /// </summary>
    public IReadOnlyList<string> entries => _entries;

    /// <summary>
    /// 创建命令历史管理器
    /// </summary>
    /// <param name="maxEntries">最大保留条目数，默认 1000</param>
    public ReplHistory(int maxEntries = 1000)
    {
        _max_entries = maxEntries;
    }

    /// <summary>
    /// 添加一条命令到历史
    /// </summary>
    /// <param name="command">命令文本</param>
    public void add(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        if (_entries.Count > 0 && _entries[^1] == command)
        {
            return;
        }

        _entries.Add(command);

        while (_entries.Count > _max_entries)
        {
            _entries.RemoveAt(0);
        }
    }

    /// <summary>
    /// 获取上一条历史（Up Arrow）
    /// </summary>
    /// <param name="index">当前历史索引的引用</param>
    /// <returns>历史文本，无更多时返回 null</returns>
    public string? get_previous(ref int index)
    {
        if (_entries.Count == 0)
        {
            return null;
        }

        if (index == -1)
        {
            index = _entries.Count - 1;
        }
        else if (index > 0)
        {
            index--;
        }

        return _entries[index];
    }

    /// <summary>
    /// 获取下一条历史（Down Arrow）
    /// </summary>
    /// <param name="index">当前历史索引的引用</param>
    /// <returns>历史文本，到达末尾时返回 null 并重置索引</returns>
    public string? get_next(ref int index)
    {
        if (_entries.Count == 0 || index == -1)
        {
            return null;
        }

        if (index < _entries.Count - 1)
        {
            index++;
            return _entries[index];
        }

        index = -1;
        return null;
    }

    /// <summary>
    /// 清空历史
    /// </summary>
    public void clear()
    {
        _entries.Clear();
    }
}
