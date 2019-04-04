namespace Core.Widget.Window;

/// <summary>
///     IToastService 接口
/// </summary>
public interface IToastService
{
    /// <summary>
    ///     显示 Toast 消息
    /// </summary>
    void show_toast(string message);
}