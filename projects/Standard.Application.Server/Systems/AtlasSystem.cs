namespace Std.App.Server.Systems;

/// <summary>
///     Atlas 业务系统抽象基类，内置四样横切服务属性。
///     所有业务系统可以选择继承它，获得开箱即用的日志、缓存、队列和事件总线能力。
/// </summary>
public abstract class AtlasSystem : IAtlasSystem
{
    /// <summary>
    ///     系统日志器，由框架自动填充
    /// </summary>
    public IAtlasSystemLogger Logger { get; internal set; } = null!;

    /// <summary>
    ///     缓存服务，由框架自动填充
    /// </summary>
    public IAtlasCache Cache { get; internal set; } = null!;

    /// <summary>
    ///     队列服务，由框架自动填充
    /// </summary>
    public IAtlasQueue Queue { get; internal set; } = null!;

    /// <summary>
    ///     事件总线，由框架自动填充
    /// </summary>
    public IAtlasEventBus EventBus { get; internal set; } = null!;
}