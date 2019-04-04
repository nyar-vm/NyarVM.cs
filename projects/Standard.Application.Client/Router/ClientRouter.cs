namespace Std.App.Client;

/// <summary>
///     客户端路由器的默认实现，提供基于路径的页面导航和历史记录
/// </summary>
public sealed class ClientRouter : IRouter
{
    private readonly List<string> _history = [];
    private int _currentIndex = -1;

    /// <summary>
    ///     初始化客户端路由器
    /// </summary>
    public ClientRouter()
    {
        _history.Add("/");
        _currentIndex = 0;
    }

    /// <inheritdoc />
    public string CurrentPath { get; private set; } = "/";

    /// <inheritdoc />
    public bool CanGoBack => _currentIndex > 0;

    /// <inheritdoc />
    public bool CanGoForward => _currentIndex < _history.Count - 1;

    /// <inheritdoc />
    public event Action<string>? OnPathChanged;

    /// <inheritdoc />
    public Task NavigateToAsync(string path)
    {
        if (path == CurrentPath) return Task.CompletedTask;

        // 清除当前位置之后的历史记录
        if (_currentIndex < _history.Count - 1)
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);

        _history.Add(path);
        _currentIndex = _history.Count - 1;
        CurrentPath = path;

        OnPathChanged?.Invoke(path);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        if (!CanGoBack) return Task.CompletedTask;

        _currentIndex--;
        CurrentPath = _history[_currentIndex];
        OnPathChanged?.Invoke(CurrentPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoForwardAsync()
    {
        if (!CanGoForward) return Task.CompletedTask;

        _currentIndex++;
        CurrentPath = _history[_currentIndex];
        OnPathChanged?.Invoke(CurrentPath);
        return Task.CompletedTask;
    }
}