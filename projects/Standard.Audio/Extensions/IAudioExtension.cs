namespace Sonic.Audio.Extensions;

/// <summary>
///     音频扩展接口，定义扩展的注册能力。
/// </summary>
public interface IAudioExtension
{
    /// <summary>
    ///     获取扩展名称。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     将扩展注册到扩展上下文中。
    /// </summary>
    /// <param name="context">音频扩展上下文。</param>
    void register(AudioExtensionContext context);
}