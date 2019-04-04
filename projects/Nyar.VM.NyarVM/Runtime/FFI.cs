using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Nyar.VM.NyarVM.Runtime;

/// <summary>
///     外部函数接口，支持加载原生共享库和调用导出函数
/// </summary>
public sealed class Ffi : IDisposable
{
    /// <summary>
    ///     已注册的外部函数（名称 → 函数指针）
    /// </summary>
    private readonly ConcurrentDictionary<string, IntPtr> _functions;

    /// <summary>
    ///     已注册的内部函数（ID → 函数委托）
    /// </summary>
    private readonly ConcurrentDictionary<uint, Delegate> _intrinsics;

    /// <summary>
    ///     已加载的库句柄（路径 → 句柄）
    /// </summary>
    private readonly ConcurrentDictionary<string, IntPtr> _libraries;

    /// <summary>
    ///     初始化 FFI
    /// </summary>
    public Ffi()
    {
        _libraries = new ConcurrentDictionary<string, IntPtr>();
        _functions = new ConcurrentDictionary<string, IntPtr>();
        _intrinsics = new ConcurrentDictionary<uint, Delegate>();
    }

    /// <summary>
    ///     释放所有资源
    /// </summary>
    public void Dispose()
    {
        foreach (var kvp in _libraries) NativeLibrary.Free(kvp.Value);

        _libraries.Clear();
        _functions.Clear();
        _intrinsics.Clear();
    }

    /// <summary>
    ///     加载原生共享库
    /// </summary>
    /// <param name="path">库文件路径。</param>
    /// <returns>库句柄。</returns>
    /// <exception cref="DllNotFoundException">库加载失败时抛出</exception>
    public IntPtr load_library(string path)
    {
        if (_libraries.TryGetValue(path, out var existingHandle)) return existingHandle;

        var handle = NativeLibrary.Load(path);
        if (handle == IntPtr.Zero) throw new DllNotFoundException($"Failed to load library: {path}");

        _libraries[path] = handle;
        return handle;
    }

    /// <summary>
    ///     获取导出函数指针
    /// </summary>
    /// <param name="library">库句柄。</param>
    /// <param name="name">函数名称。</param>
    /// <returns>函数指针。</returns>
    /// <exception cref="EntryPointNotFoundException">函数未找到时抛出</exception>
    public IntPtr get_function(IntPtr library, string name)
    {
        var ptr = NativeLibrary.GetExport(library, name);
        if (ptr == IntPtr.Zero) throw new EntryPointNotFoundException($"Function '{name}' not found in library.");

        return ptr;
    }

    /// <summary>
    ///     调用原生函数
    /// </summary>
    /// <typeparam name="T">委托类型</typeparam>
    /// <param name="functionPtr">函数指针。</param>
    /// <returns>委托实例。</returns>
    public T get_delegate<T>(IntPtr functionPtr) where T : Delegate
    {
        return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
    }

    /// <summary>
    ///     释放原生库
    /// </summary>
    /// <param name="path">库文件路径。</param>
    /// <returns>是否成功释放。</returns>
    public bool free_library(string path)
    {
        if (_libraries.TryRemove(path, out var handle))
        {
            NativeLibrary.Free(handle);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     注册外部函数
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <param name="functionPtr">函数指针。</param>
    public void register_function(string name, IntPtr functionPtr)
    {
        _functions[name] = functionPtr;
    }

    /// <summary>
    ///     解析外部函数
    /// </summary>
    /// <param name="name">函数名称。</param>
    /// <returns>函数指针，未找到返回 IntPtr.Zero</returns>
    public IntPtr resolve_function(string name)
    {
        return _functions.TryGetValue(name, out var ptr) ? ptr : IntPtr.Zero;
    }

    /// <summary>
    ///     注册内部函数
    /// </summary>
    /// <param name="id">函数 ID。</param>
    /// <param name="func">函数委托。</param>
    public void register_intrinsic(uint id, Delegate func)
    {
        _intrinsics[id] = func;
    }

    /// <summary>
    ///     获取内部函数
    /// </summary>
    /// <param name="id">函数 ID。</param>
    /// <returns>函数委托，未找到返回 null。</returns>
    public Delegate? get_intrinsic(uint id)
    {
        return _intrinsics.GetValueOrDefault(id);
    }
}