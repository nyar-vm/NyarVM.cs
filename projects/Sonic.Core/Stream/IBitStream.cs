namespace Core.Stream;

/// <summary>
///     位流接口，支持按位和按字节的读写操作
/// </summary>
public interface IBitStream
{
    /// <summary>
    ///     是否可读
    /// </summary>
    bool can_read { get; }

    /// <summary>
    ///     是否可写
    /// </summary>
    bool can_write { get; }

    /// <summary>
    ///     写入单个位
    /// </summary>
    /// <param name="value">位值</param>
    void write_bit(bool value);

    /// <summary>
    ///     读取单个位
    /// </summary>
    /// <returns>位值</returns>
    bool read_bit();

    /// <summary>
    ///     写入一个字节
    /// </summary>
    /// <param name="value">字节值</param>
    void write_byte(byte value);

    /// <summary>
    ///     读取一个字节
    /// </summary>
    /// <returns>字节值</returns>
    byte read_byte();
}