namespace Core.Widget.Window;

/// <summary>
///     IPopupService 接口
/// </summary>
public interface IPopupService
{
    /// <summary>
    ///     显示弹出窗口
    /// </summary>
    void show_popup(IDialog dialog);
}