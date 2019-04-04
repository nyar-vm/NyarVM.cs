using Std.Category;
using Std.Text;

namespace Std.Platform;

/// <summary>
///     文件 I/O 操作，提供函数式风格的文件读写
/// </summary>
public static class File
{
    /// <summary>
    ///     读取文件全部字节
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>成功返回字节数组的 Ok，失败返回错误信息的 Error</returns>
    public static Result<byte[], string> read_bytes(string path)
    {
        try
        {
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read);

            using (fs)
            {
                var bytes = new byte[fs.Length];
                fs.ReadExactly(bytes);
                return Result<byte[], string>.ok(bytes);
            }
        }
        catch (FileNotFoundException)
        {
            return Result<byte[], string>.error($"文件不存在: {path}");
        }
        catch (UnauthorizedAccessException)
        {
            return Result<byte[], string>.error($"无权访问: {path}");
        }
        catch (Exception ex)
        {
            return Result<byte[], string>.error($"读取文件失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     读取文件全部文本
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>成功返回文件内容的 Ok，失败返回错误信息的 Error</returns>
    public static Result<string, string> read_text(string path)
    {
        return read_bytes(path).map(bytes => SonicEncoding.decode_utf8(bytes));
    }

    /// <summary>
    ///     写入字节到文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="bytes">要写入的字节</param>
    /// <returns>成功返回 true 的 Ok，失败返回错误信息的 Error</returns>
    public static Result<bool, string> write_bytes(string path, ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            fs.Write(bytes);
            return Result<bool, string>.ok(true);
        }
        catch (UnauthorizedAccessException)
        {
            return Result<bool, string>.error($"无权写入: {path}");
        }
        catch (Exception ex)
        {
            return Result<bool, string>.error($"写入文件失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     写入文本到文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="text">要写入的文本</param>
    /// <returns>成功返回 true 的 Ok，失败返回错误信息的 Error</returns>
    public static Result<bool, string> write_text(string path, string text)
    {
        return write_bytes(path, SonicEncoding.encode_utf8(text));
    }

    /// <summary>
    ///     检查文件是否存在
    /// </summary>
    /// <param name="path">文件路径</param>
    public static bool exists(string path)
    {
        return System.IO.File.Exists(path);
    }

    /// <summary>
    ///     删除文件
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>成功返回 true 的 Ok，失败返回错误信息的 Error</returns>
    public static Result<bool, string> delete(string path)
    {
        try
        {
            System.IO.File.Delete(path);
            return Result<bool, string>.ok(true);
        }
        catch (FileNotFoundException)
        {
            return Result<bool, string>.error($"文件不存在: {path}");
        }
        catch (Exception ex)
        {
            return Result<bool, string>.error($"删除文件失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     获取文件大小（字节）
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <returns>成功返回文件大小的 Ok，失败返回错误信息的 Error</returns>
    public static Result<long, string> get_size(string path)
    {
        try
        {
            return Result<long, string>.ok(new FileInfo(path).Length);
        }
        catch (Exception ex)
        {
            return Result<long, string>.error($"获取文件大小失败: {ex.Message}");
        }
    }
}