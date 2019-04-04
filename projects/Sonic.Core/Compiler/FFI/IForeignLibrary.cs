namespace Core.Compiler.FFI;

/// <summary>
///     外部库接口，表示通过 FFI 绑定的原生库
/// </summary>
public interface IForeignLibrary
{
    /// <summary>
    ///     获取原生库的名称
    /// </summary>
    string library_name { get; }
}