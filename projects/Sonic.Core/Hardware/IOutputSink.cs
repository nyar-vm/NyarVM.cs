namespace Core.Hardware;

/// <summary>
///     输出接收器接口，用于呈现内容
/// </summary>
public interface IOutputSink
{
    /// <summary>
    ///     呈现内容
    /// </summary>
    /// <param name="content">要呈现的内容</param>
    void present(object content);
}